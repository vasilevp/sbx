using NAudio.Wave;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using SharpCompress.Common;
using SharpCompress.Archives;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;

using static sbx.Translator;

namespace sbx
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

        StableSelector<FileInfo> fileSelector = new(null, 9);

        Overlay<FileInfo> overlay = new();

        string tempDirectory = Path.Combine(Path.GetTempPath(), "sbx_" + Path.GetRandomFileName());

        WaveOutEvent audio = new();
        WaveOutEvent audioMonitor = new();

        public MainWindow()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // needed for cyrillic fix

            cbOutputDevice.ItemsSource = PlaybackDevices();
            cbMonitorDevice.ItemsSource = PlaybackDevices();
            audio.PlaybackStopped += (o, e) => status.Content = Translate("Ready");

            overlay.IsVisible = false;

            Directory.CreateDirectory(tempDirectory);
        }

        IEnumerable<string> PlaybackDevices()
        {
            // get proper device names because NAudio is kinda dumb
            var devices = new MMDeviceEnumerator().
                EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).
                GroupBy(x => x.FriendlyName[..Math.Min(x.FriendlyName.Length, 31)]).
                ToDictionary(x => x.Key);

            // return proper names in WaveOut order
            return
                from name in (
                    from i in Enumerable.Range(-1, WaveOut.DeviceCount + 1) select WaveOut.GetCapabilities(i).ProductName
                    )
                select devices.TryGetValue(name, out var device) && device.Count() == 1
                    ? device.First().FriendlyName
                    : name;
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

        FileInfo SelectSubset(int selection)
        {
            if (!fileSelector.Select(selection))
            {
                var subdiv = fileSelector.GetCurrentSubdivision();
                if (subdiv == null)
                    return null;

                if (subdiv.Count() > 9)
                {
                    throw new Exception($"subdivision longer than 9 ({subdiv.Count()})");
                }

                fileSelector.Unselect();
                DrawSelection(true);

                return subdiv.FirstOrDefault();
            }

            DrawSelection(false);
            return null;
        }

        void DrawSelection(bool updatePrefix)
        {
            fileGrid.Children.Clear();
            overlay.SetItems(null);

            var subdivs = fileSelector.GetSubdivisions();
            if (subdivs == null || !subdivs.Any())
            {
                return;
            }

            if (updatePrefix && Options.trimPrefix)
            {
                Options.trimAmount = 0;
                var prefix = subdivs.FirstOrDefault().FirstOrDefault().ToString();
                foreach (var part in subdivs)
                {
                    foreach (var fi in part)
                    {
                        var name = fi.ToString();
                        for (int ii = 0; ii < Math.Min(prefix.Length, name.Length); ii++)
                        {
                            if (prefix[ii] != name[ii])
                            {
                                prefix = prefix[..ii];
                                break;
                            }
                        }
                    }
                }

                Options.trimAmount = prefix.Length;
            }
            else if (updatePrefix)
            {
                Options.trimAmount = 0;
            }

            overlay.SetItems(subdivs);

            int i = 0;
            foreach (var part in subdivs)
            {
                int shift = 0;
                foreach (var f in part)
                {
                    var l = new Label
                    {
                        Content = f,
                        Margin = new Thickness { Top = shift }
                    };
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
            FileInfo result = null;
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

            if (result == null || result.Reader == null) return;

            try
            {
                result.Reader.Seek(0, SeekOrigin.Begin);
                audio.Stop();
                audio.DeviceNumber = cbOutputDevice.SelectedIndex - 1;
                var vsp = new VolumeSampleProvider(result.Reader.ToSampleProvider())
                {
                    Volume = Options.volume
                };
                audio.Init(vsp);

                if (checkboxInput.IsChecked.Value)
                {
                    result.Reader2.Seek(0, SeekOrigin.Begin);
                    audioMonitor.Stop();
                    audioMonitor.DeviceNumber = cbMonitorDevice.SelectedIndex - 1;
                    var vsp2 = new VolumeSampleProvider(result.Reader2.ToSampleProvider())
                    {
                        Volume = Options.monitorVolume
                    };
                    audioMonitor.Init(vsp2);
                    audioMonitor.Play();
                }

                audio.Play();

                Application.Current.Dispatcher.InvokeAsync(() => status.Content = Translate("Playing {0}", result));
            }
            catch (Exception e)
            {
                status.Content = $"{Translate("Error")}: {e.Message}";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            var hwnd = new WindowInteropHelper(this).Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(new HwndSourceHook(WndProc));

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

            var d = new VistaOpenFileDialog
            {
                Filter = "Archives|*.7z;*.zip;*.rar|All files|*.*"
            };
            if (!d.ShowDialog(this) ?? true)
            {
                ButtonOpenArchive.IsEnabled = true;
                ButtonOpenFolder.IsEnabled = true;
                return;
            }

            ClearDirectory();
            Directory.CreateDirectory(tempDirectory);

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
                            progress.Maximum = archive.Entries.Count();
                            progress.Value++;
                            status.Content = Translate("Read {0} ({1}/{2})", entry.Key, progress.Value, progress.Maximum);
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
            fileSelector?.Dispose();
            fileSelector = null;
            Directory.Delete(tempDirectory, true);
        }

        void LoadDirectory(string path)
        {
            var files = Directory.GetFiles(path);

            progress.Maximum = files.Length;
            progress.Value = 0;
            progress.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                var timer = new Stopwatch();
                timer.Start();

                var audioFiles = new List<FileInfo>();
                var failed = new List<Tuple<string, Exception>>();

                var i = 0;
                foreach (var f in files)
                {
                    Application.Current.Dispatcher.Invoke(() => status.Content = Translate("Loading {0} ({1}/{2})", f, progress.Value, progress.Maximum));
                    try
                    {
                        var r = StreamFactory.Create(f);
                        var r2 = StreamFactory.Create(f);
                        var fi = new FileInfo
                        {
                            Reader = r,
                            Reader2 = r2,
                            Tag = TagLib.File.Create(f).Tag,
                            FileName = f,
                        };
                        audioFiles.Add(fi);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(Tuple.Create(f, ex));
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progress.Value = i++;
                    });
                }

                fileSelector = new StableSelector<FileInfo>(audioFiles, 9);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DrawSelection(true);

                    status.Content = Translate("{0} files loaded in {1}s", audioFiles.Count, timer.Elapsed.TotalSeconds);

                    if (failed.Count > 0)
                    {
                        var failedFiles = string.Join(",", failed.Select(f => Path.GetFileName(f.Item1)));

                        status.Content += Translate("; {0} files failed to load: {1}", failed.Count, failedFiles);

                        var msg = string.Join("\n", failed.Select(f => $"{f.Item1}: {f.Item2.Message}"));
                        MessageBox.Show(msg, "Error during load", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    progress.Visibility = Visibility.Collapsed;
                    ButtonOpenArchive.IsEnabled = true;
                    ButtonOpenFolder.IsEnabled = true;
                });
            });
        }

        private void MainWindow1_Closing(object sender, CancelEventArgs e)
        {
            ClearDirectory();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new Options().ShowDialog();
            fileSelector.Unselect();
            DrawSelection(true);
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

        private void btnLangRu_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("lang\\ru-RU.xaml", UriKind.Relative)
            });

            status.Content = Translate("Language changed");
        }

        private void btnLangEn_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("lang\\en-GB.xaml", UriKind.Relative)
            });

            status.Content = Translate("Language changed");
        }
    }
}
