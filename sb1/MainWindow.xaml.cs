using NAudio.Wave;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;

namespace sb1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    enum VirtualKey
    {
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1,
        VK_NUMPAD2,
        VK_NUMPAD3,
        VK_NUMPAD4,
        VK_NUMPAD5,
        VK_NUMPAD6,
        VK_NUMPAD7,
        VK_NUMPAD8,
        VK_NUMPAD9,
        VK_MULTIPLY,
        VK_ADD,
        VK_SEPARATOR,
        VK_SUBTRACT,
    };

    enum HotKey
    {
        KP_1,
        KP_2,
        KP_3,
        KP_4,
        KP_5,
        KP_6,
        KP_7,
        KP_8,
        KP_9,
        KP_PLUS,
        KP_MINUS,
        KP_MULTIPLY,
    };


    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        MultiSelector<FileInfo> fileSelector = new MultiSelector<FileInfo>(null, 9);
        WaveOutEvent audio = new WaveOutEvent();

        Overlay<FileInfo> overlay = new Overlay<FileInfo>();

        struct FileInfo
        {
            public AudioFileReader Reader;
            public TagLib.Tag Tag;

            public override string ToString()
            {
                return Tag.Title ?? Path.GetFileName(Reader.FileName);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            combobox1.ItemsSource = EnumerateDevices();
            this.KeyUp += MainWindow_KeyUp;

            overlay.IsVisible = false;
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {

        }

        IEnumerable<string> EnumerateDevices()
        {
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                yield return caps.ProductName;
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var d = new VistaFolderBrowserDialog();
            if (!d.ShowDialog(this) ?? true)
            {
                return;
            }

            var files = Directory.GetFiles(d.SelectedPath);
            var audioFiles = new List<FileInfo>();
            foreach (var f in files)
            {
                try
                {
                    var r = new AudioFileReader(f);
                    var fi = new FileInfo();
                    fi.Reader = r;
                    fi.Tag = TagLib.File.Create(f).Tag;
                    audioFiles.Add(fi);
                }
                catch { }
            }

            fileSelector = new MultiSelector<FileInfo>(audioFiles, 9);
            //MapFiles();
            DrawSelection();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {

        }

        void MapFiles()
        {
            overlay.SetItems(fileSelector.GetSubdivisions());
            DrawSelection();
        }

        FileInfo? SelectSubset(int selection)
        {
            if (!fileSelector.Select(selection))
            {
                var subdiv = fileSelector.GetCurrentSubdivision();
                if (subdiv.Count() > 9)
                {
                    throw new Exception($"subdivision longer than 9 ({subdiv.Count()})");
                }
                var result = subdiv.FirstOrDefault();
                fileSelector.Unselect();
                DrawSelection();

                return result;
            }

            DrawSelection();
            return null;
        }

        void DrawSelection()
        {
            fileGrid.Children.Clear();

            overlay.SetItems(fileSelector.GetSubdivisions());

            int i = 0;
            foreach (var part in fileSelector.GetSubdivisions())
            {
                int shift = 0;
                foreach (var f in part)
                {
                    var l = new Label();
                    l.Content = f;
                    l.Margin = new Thickness { Top = shift };
                    Grid.SetColumn(l, i % 3);
                    Grid.SetRow(l, i / 3);


                    fileGrid.Children.Add(l);
                    shift += 10;
                }
                i++;
            }
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0312:
                    handleKey((HotKey)wParam.ToInt32());
                    break;
            }
            return IntPtr.Zero;
        }

        private void handleKey(HotKey key)
        {
            FileInfo? result = null;
            switch (key)
            {
                case HotKey.KP_7:
                    result = SelectSubset(0);
                    break;
                case HotKey.KP_8:
                    result = SelectSubset(1);
                    break;
                case HotKey.KP_9:
                    result = SelectSubset(2);
                    break;
                case HotKey.KP_4:
                    result = SelectSubset(3);
                    break;
                case HotKey.KP_5:
                    result = SelectSubset(4);
                    break;
                case HotKey.KP_6:
                    result = SelectSubset(5);
                    break;
                case HotKey.KP_1:
                    result = SelectSubset(6);
                    break;
                case HotKey.KP_2:
                    result = SelectSubset(7);
                    break;
                case HotKey.KP_3:
                    result = SelectSubset(8);
                    break;
                case HotKey.KP_MINUS:
                    audio.Stop();
                    break;
                case HotKey.KP_PLUS:
                    overlay.IsVisible = !overlay.IsVisible;
                    break;
                case HotKey.KP_MULTIPLY:
                    overlay.SwitchFontSize();
                    break;
            }

            if (result == null || result.Value.Reader == null) return;

            try
            {
                result.Value.Reader.Seek(0, SeekOrigin.Begin);
                audio.Stop();
                audio.DeviceNumber = combobox1.SelectedIndex - 1;
                audio.Init(result.Value.Reader);
                audio.Play();
                status.Content = $"Playing {result.ToString()}";
            }
            catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            var hwnd = new WindowInteropHelper(this).Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(new HwndSourceHook(WndProc));

            Task.Run(() => overlay.Run());

            RegisterHotKey(hwnd, (int)HotKey.KP_1, 0, (int)VirtualKey.VK_NUMPAD1);
            RegisterHotKey(hwnd, (int)HotKey.KP_2, 0, (int)VirtualKey.VK_NUMPAD2);
            RegisterHotKey(hwnd, (int)HotKey.KP_3, 0, (int)VirtualKey.VK_NUMPAD3);
            RegisterHotKey(hwnd, (int)HotKey.KP_4, 0, (int)VirtualKey.VK_NUMPAD4);
            RegisterHotKey(hwnd, (int)HotKey.KP_5, 0, (int)VirtualKey.VK_NUMPAD5);
            RegisterHotKey(hwnd, (int)HotKey.KP_6, 0, (int)VirtualKey.VK_NUMPAD6);
            RegisterHotKey(hwnd, (int)HotKey.KP_7, 0, (int)VirtualKey.VK_NUMPAD7);
            RegisterHotKey(hwnd, (int)HotKey.KP_8, 0, (int)VirtualKey.VK_NUMPAD8);
            RegisterHotKey(hwnd, (int)HotKey.KP_9, 0, (int)VirtualKey.VK_NUMPAD9);
            RegisterHotKey(hwnd, (int)HotKey.KP_PLUS, 0, (int)VirtualKey.VK_ADD);
            RegisterHotKey(hwnd, (int)HotKey.KP_MINUS, 0, (int)VirtualKey.VK_SUBTRACT);
            RegisterHotKey(hwnd, (int)HotKey.KP_MULTIPLY, 0, (int)VirtualKey.VK_MULTIPLY);
        }
    }
}
