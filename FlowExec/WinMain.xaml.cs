using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System.Collections.Generic;
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
        // 输入框分隔符
        private const string DefaultDelimiters = "/\\.,;:|!@#$%^&*()+=[]{}<>\"\'~`? \t\n\r";

        private string _currentIconPath = "";

        [DllImport("shell32.dll")]
        public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, int[] phiconLarge, int[] phiconSmall, uint nIcons);

        public WinMain()
        {
            InitializeComponent();

            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            var attribute = Dwm.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = Dwm.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            Dwm.DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));

            var widthRatio = Properties.Settings.Default.Width * 0.01;
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
                InputBar.Focus();
            };
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

            animation.Completed += (s, _) => Environment.Exit(0);
            this.BeginAnimation(Window.TopProperty, animation);
        }

        private void InputBar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                DeletePreviousWord();
            }
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;

                if (!string.IsNullOrEmpty(InputBar.Text))
                {
                    HandleUserInput(InputBar.Text, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                }
            }
            if (e.Key == Key.Escape && InputBar.Text.Length == 0)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void DeletePreviousWord()
        {
            int caretIndex = InputBar.CaretIndex;
            if (caretIndex == 0) return;

            string text = InputBar.Text;
            int startIndex = caretIndex;
            bool isFirstDelimiter = true;

            while (startIndex > 0)
            {
                startIndex--;
                if (IsDelimiter(text[startIndex]))
                {
                    if (!isFirstDelimiter)
                    {
                        startIndex++;
                        break;
                    }
                }
                else if (isFirstDelimiter)
                {
                    isFirstDelimiter = false;
                }
            }

            // 执行删除操作
            InputBar.Text = text.Remove(startIndex, caretIndex - startIndex);
            InputBar.CaretIndex = startIndex;
        }

        private bool IsDelimiter(char c) => DefaultDelimiters.Contains(c);

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
                HandleUserInput(InputBar.Text);
        }

        private void HandleUserInput(string input, bool admin = false)
        {
            var argv = CommandLine.Parse(input);
            Debug.WriteLine(argv.Count);

            if (argv[0].StartsWith("$"))
            {
                // 内建命令
                switch (argv[0].ToLower())
                {
                    case "$":
                        var wnd = new WinOption();
                        wnd.Owner = this;
                        ThemeManager.SetRequestedTheme(wnd, App.CurrentTheme);
                        wnd.ShowDialog();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
