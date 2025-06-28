using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlowExec
{
    public partial class WinOption : Window
    {
        public WinOption()
        {
            InitializeComponent();
            ComboTheme.SelectedIndex = Properties.Settings.Default.Theme;

            var backdropIndex = 1;
            switch (Properties.Settings.Default.Backdrop)
            {
                case "None":
                    backdropIndex = 1;
                    break;
                case "Mica":
                    backdropIndex = 2;
                    break;
                case "Acrylic":
                    backdropIndex = 3;
                    break;
                case "Tabbed":
                    backdropIndex = 4;
                    break;
            }
            ComboBackdrop.SelectedIndex = backdropIndex - 1;

            foreach (var i in App.AvailableLanguages)
            {
                ComboLanguage.Items.Add(i.Value);
            }
            ComboLanguage.SelectedValue = 
                Properties.Settings.Default.Language == "Default" ?
                App.AvailableLanguages[CultureInfo.CurrentUICulture.Name] :
                App.AvailableLanguages[Properties.Settings.Default.Language];

            SliderWidth.Value = Properties.Settings.Default.WidthRatio;
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = (ElementTheme)ComboTheme.SelectedIndex;
            App.SetWindowTheme(this, theme);
            App.SetWindowTheme(App.BaseWindow, theme);
        }

        private void ComboBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var backdrop = (iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType) (ComboBackdrop.SelectedIndex + 1);
            WindowHelper.SetSystemBackdropType(this, backdrop);
            WindowHelper.SetSystemBackdropType(App.BaseWindow, backdrop);
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
