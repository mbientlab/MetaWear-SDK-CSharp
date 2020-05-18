using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StarterApp {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DeviceSetup : Page {
        private IMetaWearBoard metawear;

        public DeviceSetup() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
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
