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
        public static readonly ElementTheme DefaultTheme = IsSystemUsingDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
        public static ElementTheme CurrentTheme;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeComponent();

            CurrentTheme =
                FlowExec.Properties.Settings.Default.Theme == 0 ?
                DefaultTheme :
                (ElementTheme) FlowExec.Properties.Settings.Default.Theme;

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

            var wnd = new WinMain();
            ThemeManager.SetRequestedTheme(wnd, CurrentTheme);
            wnd.Show();
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
