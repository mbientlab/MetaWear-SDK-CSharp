using MbientLab.MetaWear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MbientLab.BtleDeviceScanner {
    public sealed class MacAddressHexString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            string hexString = ((ulong)value).ToString("X");
            return hexString.Insert(2, ":").Insert(5, ":").Insert(8, ":").Insert(11, ":").Insert(14, ":");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
    public sealed class ConnectionStateColor : IValueConverter {
        public SolidColorBrush ConnectedColor { get; set; }
        public SolidColorBrush DisconnectedColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language) {
            switch ((BluetoothConnectionStatus)value) {
                case BluetoothConnectionStatus.Connected:
                    return ConnectedColor;
                case BluetoothConnectionStatus.Disconnected:
                    return DisconnectedColor;
                default:
                    throw new MissingMemberException("Unrecognized connection status: " + value.ToString());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }

    public class ScanConfig {
        internal int Duration { get; }
        internal Type NextPageType { get; }
        internal List<Guid> ServiceUuids { get; }

        public ScanConfig(Type nextPageType, int duration = 10000, List<Guid> serviceUuids = null) {
            Duration = duration;
            NextPageType = nextPageType;
            ServiceUuids = serviceUuids == null ? new List<Guid>(new Guid[] { Constants.METAWEAR_GATT_SERVICE }) : serviceUuids;
        }
    }

    /// <summary>
    /// Page that scans for and shows nearby Bluetooth LE devices.
    /// </summary>
    public sealed partial class ScannerPage : Page {
        private BluetoothLEAdvertisementWatcher btleWatcher;
        private HashSet<ulong> seenDevices = new HashSet<ulong>();
        private ScanConfig config;
        private Timer timer;

        public ScannerPage() {
            InitializeComponent();

            btleWatcher = new BluetoothLEAdvertisementWatcher {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            btleWatcher.Received += async (w, btAdv) => {
                if (!seenDevices.Contains(btAdv.BluetoothAddress) && 
                        config.ServiceUuids.Aggregate(true, (acc, e) => acc & btAdv.Advertisement.ServiceUuids.Contains(e))) {
                    seenDevices.Add(btAdv.BluetoothAddress);
                    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                    if (device != null) {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => pairedDevices.Items.Add(device));
                    }
                }
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            config = e.Parameter as ScanConfig;
            refreshDevices_Click(null, null);
        }

        /// <summary>
        /// Callback for the refresh button which populates the devices list
        /// </summary>
        private void refreshDevices_Click(object sender, RoutedEventArgs args) {
            if (timer != null) {
                timer.Dispose();
                timer = null;
            }
            btleWatcher.Stop();

            var connected = pairedDevices.Items.Where(e => (e as BluetoothLEDevice).ConnectionStatus == BluetoothConnectionStatus.Connected);

            seenDevices.Clear();
            pairedDevices.Items.Clear();

            foreach (var it in connected) {
                seenDevices.Add((it as BluetoothLEDevice).BluetoothAddress);
                pairedDevices.Items.Add(it);
            }

            btleWatcher.Start();
            timer = new Timer(e => btleWatcher.Stop(), null, config.Duration, Timeout.Infinite);
        }

        /// <summary>
        /// Callback for the devices list which navigates to the <see cref="DeviceSetup"/> page with the selected device
        /// </summary>
        private async void pairedDevices_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            btleWatcher.Stop();
            var item = ((ListView)sender).SelectedItem as BluetoothLEDevice;

            if (item != null) {
                ContentDialog initPopup = new ContentDialog() {
                    Title = "Initializing API",
                    Content = "Please wait while the app initializes the API"
                };

                initPopup.ShowAsync();
                var board = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(item);
                await board.InitializeAsync();
                initPopup.Hide();

                Frame.Navigate(config.NextPageType, item);
            }
        }
    }
}
