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

namespace sb1
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options()
        {
            InitializeComponent();
            cbCyrillicFix.IsChecked = MainWindow.cyrillicFix;
            cbCyrillicFix.IsEnabled = MainWindow.useTags;
            cbUseIDv3Tags.IsChecked = MainWindow.useTags;
        }

        private void cbUseIDv3Tags_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.useTags = cbUseIDv3Tags.IsChecked.Value;
            cbCyrillicFix.IsEnabled = MainWindow.useTags;
        }

        private void cbCyrillicFix_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.cyrillicFix = cbCyrillicFix.IsChecked.Value;
        }
    }
}
