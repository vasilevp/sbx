using System.Windows;

namespace sbx
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {

        internal static int trimAmount = 0;
        internal static bool trimPrefix = false;
        internal static bool cyrillicFix = false;
        internal static bool useTags = false;
        internal static float volume = 1.0f;
        internal static float monitorVolume = 1.0f;

        public Options()
        {
            InitializeComponent();
            cbCyrillicFix.IsChecked = cyrillicFix;
            cbCyrillicFix.IsEnabled = useTags;
            cbUseIDv3Tags.IsChecked = useTags;
            cbTrimPrefix.IsChecked = trimPrefix;

            slVolume.Value = volume;
            slVolume.ValueChanged += Slider_ValueChanged;

            slLoopbackVolume.Value = monitorVolume;
            slLoopbackVolume.ValueChanged += SliderMonitor_ValueChanged;
        }

        private void cbUseIDv3Tags_Click(object sender, RoutedEventArgs e)
        {
            useTags = cbUseIDv3Tags.IsChecked.Value;
            cbCyrillicFix.IsEnabled = useTags;
        }

        private void cbCyrillicFix_Click(object sender, RoutedEventArgs e)
        {
            cyrillicFix = cbCyrillicFix.IsChecked.Value;
        }

        private void cbTrimPrefix_Click(object sender, RoutedEventArgs e)
        {
            trimPrefix = cbTrimPrefix.IsChecked.Value;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volume = (float)e.NewValue;
        }
        private void SliderMonitor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            monitorVolume = (float)e.NewValue;
        }
    }
}
