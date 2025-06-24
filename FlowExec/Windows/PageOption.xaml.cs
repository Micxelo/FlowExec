using iNKORE.UI.WPF.Modern;
using System;
using System.Collections.Generic;
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
    public partial class PageOption : Page
    {
        public PageOption()
        {
            InitializeComponent();
            ComboTheme.SelectedIndex = Properties.Settings.Default.Theme;
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = (ElementTheme)ComboTheme.SelectedIndex;
            App.SetWindowTheme(this, theme);
            App.SetWindowTheme(App.BaseWindow, theme);
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SliderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void CardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageShortcuts());
        }
    }
}
