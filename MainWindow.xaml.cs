using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace SteamAutoShutdown
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IEnumerable<MyProcess> steamProcesses;

        private Stopwatch inactivityStatus = new Stopwatch();

        private bool isDownloading = false;

        private bool isRunning = true;

        private bool connectionDropped = false;

        private int currentInactivityTime = 1;

        private int action = 0;

        private int maxInactivityTime = 0;

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public MainWindow()
        {
            InitializeComponent();

            List<LibPcapLiveDevice> devicesList = new List<LibPcapLiveDevice>();

            foreach (LibPcapLiveDevice device in CaptureDeviceList.Instance)
            {
                if (device.Name != null && device.Addresses.Count > 0)
                    devicesList.Add(device);
            }

            NetworkInterfaceCombo.ItemsSource = devicesList;

            try
            {
                Process steamRunning = Process.GetProcessesByName("Steam")[0];

                steamProcesses = SteamDetect.GetProcesses().Where(p => p.Name.Equals(steamRunning.ProcessName));
            }
            catch { }
        }

        private void ToggleChanged(object sender, RoutedEventArgs e)
        {
            MyToggleButton toggleButton = (MyToggleButton)sender;

            if (toggleButton != null)
            {
                Status.Foreground = toggleButton.ToggleBackground;

                if (toggleButton.IsChecked)
                {
                    Status.Text = "Auto Shutdown Enabled";

                    isRunning = true;

                    LibPcapLiveDevice? dev = NetworkInterfaceCombo.SelectedItem as LibPcapLiveDevice;

                    action = ActionComboBox.SelectedIndex;

                    switch (TimeType.SelectedIndex)
                    {
                        case 0:
                            int.TryParse(Inactivity.Text, out maxInactivityTime);
                            break;
                        case 1:
                            int.TryParse(Inactivity.Text, out maxInactivityTime);
                            maxInactivityTime *= 60;
                            break;
                    }

                    if (dev != null)
                    {
                        _ = Task.Run(() => { Start(dev, maxInactivityTime); });
                    }
                }
                else
                {
                    Status.Text = "Auto Shutdown Disabled";

                    isRunning = false;
                }
            }
        }

        private void Start(LibPcapLiveDevice selectedInterface, int inactivityTime)
        {
            selectedInterface.Open();

            selectedInterface.OnPacketArrival += new PacketArrivalEventHandler(PacketArrival);

            selectedInterface.StartCapture();

            inactivityStatus = Stopwatch.StartNew();

            _ = Task.Run(() =>
            {
                while (isRunning)
                {
                    if (isDownloading)
                    {
                        DownloadStatus.Dispatcher.Invoke(() => { DownloadStatus.Text = "Steam Game Downloading..."; DownloadStatus.Foreground = Brushes.LimeGreen; });
                    }
                    else
                    {
                        DownloadStatus.Dispatcher.Invoke(() => { DownloadStatus.Text = $"No steam download detected {currentInactivityTime} / {maxInactivityTime} s"; DownloadStatus.Foreground = Brushes.IndianRed; });
                    }
                }

                DownloadStatus.Dispatcher.Invoke(() => { DownloadStatus.Text = "Download detection inactive"; DownloadStatus.Foreground = Brushes.IndianRed; });
            });

            _ = Task.Run(async () =>
            {
                Process steamRunning = Process.GetProcessesByName("Steam")[0];

                while (isRunning)
                {
                    steamProcesses = SteamDetect.GetProcesses().Where(p => p.Name.Equals(steamRunning.ProcessName));

                    await Task.Delay(1000);
                }
            });

            _ = Task.Run(() =>
            {
                while (isRunning)
                {
                    currentInactivityTime = (int)inactivityStatus.Elapsed.TotalSeconds;

                    if (inactivityStatus.Elapsed.TotalSeconds > 10 && isDownloading)
                    {
                        isDownloading = false;

                        inactivityStatus.Restart();
                    }

                    if (inactivityStatus.Elapsed.TotalSeconds > inactivityTime)
                    {
                        isRunning = false;

                        switch (action)
                        {
                            case 0:
                                Process.Start("shutdown", "/s /t 60");
                                break;
                            case 1:
                                Process.Start("shutdown", "/h");
                                break;
                            case 2:
                                SetSuspendState(false, true, true);
                                break;
                        }
                    }

                }

                selectedInterface.StopCapture();
            });

            _ = Task.Run(() => 
            {
                Ping p = new Ping();

                while (true)
                {
                    try
                    {
                        PingReply reply = p.Send("www.google.it");

                        if (connectionDropped && reply.Status == IPStatus.Success)
                        {
                            connectionDropped = false;

                            isRunning = true;

                            Start(selectedInterface, inactivityTime);
                        }

                        Task.Delay(5000).Wait();
                    }
                    catch 
                    {
                        DownloadStatus.Dispatcher.Invoke(() => { DownloadStatus.Text = "Connection dropped, waiting..."; DownloadStatus.Foreground = Brushes.LightYellow; });

                        isRunning = false;

                        connectionDropped = true;
                    }                 
                }
            });

        }

        private void PacketArrival(object sender, PacketCapture packetCapture)
        {
            EthernetPacket ether = (EthernetPacket)Packet.ParsePacket(LinkLayers.Ethernet, packetCapture.GetPacket().Data);

            if (ether.PayloadPacket is IPv4Packet ip)
            {
                if (ip.PayloadPacket is TcpPacket tcp)
                {
                    if (steamProcesses.Select(p => p.Port).Contains(tcp.DestinationPort) && tcp.SourcePort == 80)
                    {                       
                        inactivityStatus.Restart();

                        isDownloading = true;
                    }
                }
            }
        }
    }
}