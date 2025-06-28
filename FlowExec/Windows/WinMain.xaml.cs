using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlowExec
{
    public partial class WinMain : Window
    {
        private string _currentIconPath = "";

        // Alias 配置
        private readonly AliasConfigService _aliasService = new();
        private List<string> _aliases = new();

        public WinMain()
        {
            InitializeComponent();

            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            var attribute = Dwm.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = Dwm.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            Dwm.DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));

            var widthRatio = Properties.Settings.Default.WidthRatio * 0.01;
            var scrnWidth = SystemParameters.WorkArea.Width;
            this.Width = (int) (scrnWidth * widthRatio);
            this.MinWidth = this.Width;
            this.MaxWidth = this.Width;

            this.Top = -this.Height;
            this.Left = (scrnWidth - this.Width) / 2;

            this.Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PerformAnimateIn();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                InputBar.IsFocused = true;
                RefreshCompletionItems();
            };

            InputBar.PreviewKeyDown += InputBar_PreviewKeyDown;
            InputBar.TextChanged += InputBar_TextChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 订阅别名更新事件
            _aliasService.AliasesUpdated += OnAliasesUpdated;
        }

        private void OnAliasesUpdated(object? sender, AliasUpdatedEventArgs e)
        {
            // 根据更新类型处理
            switch (e.UpdateType)
            {
                case AliasUpdateType.InitialLoad:
                    Debug.WriteLine($"初始加载完成，共 {e.UpdatedAliases?.Count ?? 0} 个别名");
                    break;
                case AliasUpdateType.Added:
                    Debug.WriteLine($"添加别名: {e.AliasName} → {e.UpdatedAliases?[e.AliasName]}");
                    break;
                case AliasUpdateType.Updated:
                    Debug.WriteLine($"更新别名: {e.AliasName} → {e.UpdatedAliases?[e.AliasName]}");
                    break;
                case AliasUpdateType.Removed:
                    Debug.WriteLine($"删除别名: {e.AliasName}");
                    break;
            }
            RefreshCompletionItems();
        }

        private void RefreshCompletionItems()
        {
            _aliases.Clear();
            foreach (var item in _aliasService.GetAllAliases())
                _aliases.Add(item.Key);
            InputBar.CompletionItems = _aliases;
        }

        private void PerformAnimateIn()
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = 2, // 目标位置：屏幕顶部
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new ElasticEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Oscillations = 0,    // 弹性振荡次数
                    Springiness = 2      // 弹性强度
                }
            };

            this.BeginAnimation(Window.TopProperty, animation);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // 阻止立即关闭

            DoubleAnimation animation = new DoubleAnimation
            {
                To = -this.Height,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new ElasticEase
                {
                    EasingMode = EasingMode.EaseIn,
                    Oscillations = 0,
                    Springiness = 2
                }
            };

            this.BeginAnimation(Window.TopProperty, animation);

            animation.Completed += (s, _) => { _aliasService.Dispose(); Environment.Exit(0); };
            this.BeginAnimation(Window.TopProperty, animation);
        }

        public void PerformAnimateWidth(double targetWidth, double durationSeconds = 0.3)
        {
            // 创建宽度动画
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
            };

            // 应用动画
            this.BeginAnimation(Window.WidthProperty, widthAnimation);
        }

        private void InputBar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(InputBar.Text))
                    RunInput((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
            }
            if (e.Key == Key.Escape && InputBar.Text.Length == 0)
            {
                this.Close();
            }
        }

        private void InputBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var path = InputBar.Text;
            var AppIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/app.ico"));
            var icon = IconHelper.GetIcon(path);
            if (icon.Item1)
                if (_currentIconPath != InputBar.Text) { }
            else
                BtnIconImage.Source = AppIcon;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InputBar.Text))
                RunInput();
        }

        private void RunInput(bool admin = false)
        {
            var argv = InputBar.Parse();
            Debug.WriteLine(argv.Count);

            if (argv[0].StartsWith("$"))
            {
                // 内建命令

                switch (argv[0].ToLower())
                {
                    case "$":
                        var wnd = new WinOption();
                        wnd.Owner = this;
                        App.SetWindowTheme(wnd, (ElementTheme)Properties.Settings.Default.Theme);
                        wnd.ShowDialog();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
