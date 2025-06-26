using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace FlowExec
{
    public partial class App : Application
    {
        public static WinMain? BaseWindow { get; private set; }

        public static readonly Dictionary<string, string> AvailableLanguages = new Dictionary<string, string>
        {
            { "en-US", "English (US)" },
            { "en-UK", "English (UK) "},
            { "zh-CN", "简体中文" },
            { "zh-HK", "繁體中文（香港）" },
            { "zh-TW", "繁體中文（台灣）" },
        };

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeComponent();

            string requestedCulture = string.Format(@"/Localizations/StringResources.{0}.xaml", CultureInfo.CurrentUICulture.Name);
            var dict = new ResourceDictionary();
            try
            {
                dict.Source = new Uri(requestedCulture, UriKind.Relative);
            }
            catch (IOException)
            {
                dict.Source = new Uri("/Localizations/StringResources.xaml", UriKind.Relative);
            }
            Resources.MergedDictionaries.Add(dict);

            BaseWindow = new WinMain();
            SetWindowTheme(BaseWindow, (ElementTheme)FlowExec.Properties.Settings.Default.Theme);
            BaseWindow = BaseWindow;
            BaseWindow.Show();
        }

        public static void SetWindowTheme(FrameworkElement? element, ElementTheme theme)
        {
            var sysTheme = 
                IsSystemUsingDarkTheme() ?
                ElementTheme.Dark : ElementTheme.Light;
            ThemeManager.SetRequestedTheme(element, 
                theme == ElementTheme.Default ? sysTheme : theme);
        }

        private static bool IsSystemUsingDarkTheme()
        {
            const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string registryValueName = "AppsUseLightTheme";

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
            {
                object? value = key?.GetValue(registryValueName);
                return value is int useLightTheme && useLightTheme == 0;
            }
        }
    }
}
