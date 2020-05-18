using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FreeFall_Detector {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetectorSetup : Page {
        private IMetaWearBoard metawear;

        public DetectorSetup() {
            InitializeComponent();
        }
        
        protected override async void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);

            var accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Configure(odr: 50f);
            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Map(Function1.Rss).LowPass(4).Find(Threshold.Binary, 0.5f)
                    .Multicast()
                        .To().Filter(Comparison.Eq, -1).Log(data => System.Diagnostics.Debug.WriteLine("In FreeFall"))
                        .To().Filter(Comparison.Eq, 1).Log(data => System.Diagnostics.Debug.WriteLine("Not in FreeFall"))
            );
        }

        private void start_Click(object sender, RoutedEventArgs e) { 
            metawear.GetModule<ILogging>().Start();

            var accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Acceleration.Start();
            accelerometer.Start();
        }

        private void stop_Click(object sender, RoutedEventArgs e) {
            metawear.GetModule<ILogging>().Stop();

            var accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Stop();
            accelerometer.Acceleration.Stop();
        }

        private async void download_Click(object sender, RoutedEventArgs e) {
            await metawear.GetModule<ILogging>().DownloadAsync();
            System.Diagnostics.Debug.WriteLine("Log Download Completed!");
        }

        private void back_Click(object sender, RoutedEventArgs e) {
            if (!metawear.InMetaBootMode) {
                metawear.TearDown();
                metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }
    }
}
