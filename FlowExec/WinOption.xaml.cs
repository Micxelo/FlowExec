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
using System.Windows.Shapes;

namespace FlowExec
{
    public partial class WinOption : Window
    {
        public WinOption()
        {
            InitializeComponent();

            ComboTheme.Items.Add(Application.Current.FindResource("wOpt_Theme_System").ToString());
            ComboTheme.Items.Add(Application.Current.FindResource("wOpt_Theme_Light").ToString());
            ComboTheme.Items.Add(Application.Current.FindResource("wOpt_Theme_Dark").ToString());

            ComboTheme.SelectedIndex = (int) App.CurrentTheme;
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = (ElementTheme) ComboTheme.SelectedIndex;
            ThemeManager.SetRequestedTheme(this, theme);
            ThemeManager.SetRequestedTheme(this.Owner, theme);
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SliderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
