using NAudio.Wave;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using SharpCompress.Common;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;

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

        StableSelector<FileInfo> fileSelector = new StableSelector<FileInfo>(null, 9);

        Overlay<FileInfo> overlay = new Overlay<FileInfo>();

        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        internal static int trimAmount = 0;
        internal static bool trimPrefix = false;
        internal static bool cyrillicFix = true;
        internal static bool useTags = false;
        internal static float volume = 1.0f;
        internal static float monitorVolume = 1.0f;

        WaveOutEvent audio = new WaveOutEvent();
        WaveOutEvent audioMonitor = new WaveOutEvent();

        WaveIn loopbackSourceStream = null;
        BufferedWaveProvider loopbackWaveProvider = null;
        WaveOut loopbackWaveOut = null;

        struct FileInfo
        {
            public WaveStream Reader;
            public WaveStream Reader2;
            public TagLib.Tag Tag;
            public string FileName;

            private static Encoding enc1251 = Encoding.GetEncoding(1251);
            private static Encoding enc1252 = Encoding.GetEncoding(1252);

            private string process(string s)
            {
                if (trimAmount >= s.Count()) return s.Trim();

                return s.Substring(trimAmount).Trim();
            }

            public override string ToString()
            {
                var fname = Path.GetFileNameWithoutExtension(FileName);
                if (Tag.Title == null || !useTags)
                {
                    return process(fname);
                }

                if (!cyrillicFix)
                {
                    return process(Tag.Title);
                }

                return process(enc1251.GetString(enc1252.GetBytes(Tag.Title)));
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            cbOutputDevice.ItemsSource = PlaybackDevices();
            cbMonitorDevice.ItemsSource = PlaybackDevices();
            audio.PlaybackStopped += (o, e) => status.Content = "Ready";

            overlay.IsVisible = false;

            Directory.CreateDirectory(tempDirectory);
        }

        IEnumerable<string> PlaybackDevices()
        {
            var devices = new MMDeviceEnumerator().
                EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).
                GroupBy(x => x.FriendlyName.Substring(0, Math.Min(x.FriendlyName.Length, 31))). // take first 32 letters
                ToDictionary(x => x.Key);

            // get proper device names because NAudio is kinda dumb
            return
                from cap in (
                    from i in Enumerable.Range(-1, WaveOut.DeviceCount + 1) select WaveOut.GetCapabilities(i).ProductName
                    )
                select "Out: " + (devices.ContainsKey(cap) && devices[cap].Count() == 1 ? devices[cap].First().FriendlyName : cap);
        }

        private void ButtonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            ButtonOpenArchive.IsEnabled = false;
            ButtonOpenFolder.IsEnabled = false;

            var d = new VistaFolderBrowserDialog();
            if (!d.ShowDialog(this) ?? true)
            {
                ButtonOpenArchive.IsEnabled = true;
                ButtonOpenFolder.IsEnabled = true;

                return;
            }

            LoadDirectory(d.SelectedPath);
        }

        FileInfo? SelectSubset(int selection)
        {
            if (!fileSelector.Select(selection))
            {
                var subdiv = fileSelector.GetCurrentSubdivision();
                if (subdiv == null) return null;

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
            overlay.SetItems(null);

            var subdivs = fileSelector.GetSubdivisions();
            if (subdivs == null || !subdivs.Any())
            {
                return;
            }

            if (trimPrefix)
            {
                trimAmount = 0;
                var prefix = subdivs.FirstOrDefault().FirstOrDefault().ToString();
                foreach (var part in subdivs)
                {
                    foreach (var fi in part)
                    {
                        var name = fi.ToString();
                        for (int ii = 0; ii < Math.Min(prefix.Count(), name.Count()); ii++)
                        {
                            if (prefix[ii] != name[ii])
                            {
                                prefix = prefix.Substring(0, ii);
                                break;
                            }
                        }
                    }
                }

                trimAmount = prefix.Count();
            }
            else
            {
                trimAmount = 0;
            }

            overlay.SetItems(subdivs);

            int i = 0;
            foreach (var part in subdivs)
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
                    audioMonitor.Stop();
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
                audio.DeviceNumber = cbOutputDevice.SelectedIndex - 1;
                var vsp = new VolumeSampleProvider(result.Value.Reader.ToSampleProvider());
                vsp.Volume = volume;
                audio.Init(vsp);

                if (checkboxInput.IsChecked.Value)
                {
                    result.Value.Reader2.Seek(0, SeekOrigin.Begin);
                    audioMonitor.Stop();
                    audioMonitor.DeviceNumber = cbMonitorDevice.SelectedIndex - 1;
                    var vsp2 = new VolumeSampleProvider(result.Value.Reader2.ToSampleProvider());
                    vsp2.Volume = monitorVolume;
                    audioMonitor.Init(vsp2);
                    audioMonitor.Play();
                }

                audio.Play();

                Application.Current.Dispatcher.InvokeAsync(() => status.Content = $"Playing {result.ToString()}");
            }
            catch (Exception e)
            {
                status.Content = $"Error: {e.Message}";
            }
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

        private void ButtonOpenArchive_Click(object sender, RoutedEventArgs e)
        {
            ButtonOpenArchive.IsEnabled = false;
            ButtonOpenFolder.IsEnabled = false;

            var d = new VistaOpenFileDialog();
            d.Filter = "Archives|*.7z;*.zip;*.rar|All files|*.*";
            if (!d.ShowDialog(this) ?? true)
            {
                ButtonOpenArchive.IsEnabled = true;
                ButtonOpenFolder.IsEnabled = true;
                return;
            }

            ClearDirectory();
            Directory.CreateDirectory(tempDirectory);

            using (var stream = d.OpenFile())
            using (var archive = ArchiveFactory.Open(stream))
                progress.Maximum = archive.Entries.Count();

            progress.Value = 0;
            progress.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                using (var stream = d.OpenFile())
                using (var archive = ArchiveFactory.Open(stream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.IsDirectory)
                        {
                            continue;
                        }

                        entry.WriteToDirectory(tempDirectory, new ExtractionOptions()
                        {
                            ExtractFullPath = false,
                            Overwrite = true
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            progress.Value++;
                            status.Content = $"Read {entry.Key} ({progress.Value}/{progress.Maximum})";
                        });
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadDirectory(tempDirectory);
                });
            });
        }

        void ClearDirectory()
        {
            fileSelector = null;
            GC.Collect();
            Directory.Delete(tempDirectory, true);
        }

        void LoadDirectory(string path)
        {
            var files = Directory.GetFiles(path);

            progress.Maximum = files.Count();
            progress.Value = 0;
            progress.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                var timer = new System.Diagnostics.Stopwatch();
                timer.Start();

                var audioFiles = new List<FileInfo>();
                var failed = new List<string>();

                var i = 0;
                foreach (var f in files)
                {
                    Application.Current.Dispatcher.Invoke(() => status.Content = $"Loading {f} ({progress.Value}/{progress.Maximum})...");
                    try
                    {
                        var r = new AudioFileReader(f);
                        var r2 = new AudioFileReader(f);
                        var fi = new FileInfo
                        {
                            Reader = r,
                            Reader2 = r2,
                            Tag = TagLib.File.Create(f).Tag,
                            FileName = f,
                        };
                        audioFiles.Add(fi);
                    }
                    catch (Exception)
                    {
                        failed.Add(f);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progress.Value = i++;
                    });
                }

                fileSelector = new StableSelector<FileInfo>(audioFiles, 9);
                //MapFiles();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DrawSelection();

                    status.Content = $"{audioFiles.Count} files loaded in {timer.Elapsed.TotalSeconds}s";

                    if (failed.Count > 0)
                    {
                        var failedFiles = string.Join(",", failed.Select(f => Path.GetFileName(f)));

                        status.Content += $"; {failed.Count} files failed to load: {failedFiles}";
                    }

                    progress.Visibility = Visibility.Collapsed;
                    ButtonOpenArchive.IsEnabled = true;
                    ButtonOpenFolder.IsEnabled = true;
                });
            });
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClearDirectory();
        }

        private IEnumerable<IArchiveEntry> OpenArchive(Stream stream)
        {
            var options = new SharpCompress.Readers.ReaderOptions() { LeaveStreamOpen = false };

            if (SevenZipArchive.IsSevenZipFile(stream))
            {
                return SevenZipArchive.Open(stream, options).Entries;
            }

            return null;
        }

        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var o = new Options();
            o.ShowDialog();

            DrawSelection();
        }

        private void loopbackSourceStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (loopbackWaveProvider != null && loopbackWaveProvider.BufferedDuration.TotalMilliseconds <= 100)
                loopbackWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void cbOutputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            audio.Stop();
            audioMonitor.Stop();
        }

        private void checkboxInput_Click(object sender, RoutedEventArgs e)
        {
            if (checkboxInput.IsChecked.Value)
            {
                cbMonitorDevice.IsEnabled = true;
                return;
            }

            cbMonitorDevice.IsEnabled = false;
            audioMonitor.Stop();
        }
    }
}
