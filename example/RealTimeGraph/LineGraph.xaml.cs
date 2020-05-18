using OxyPlot;
using OxyPlot.Series;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using OxyPlot.Axes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RealTimeGraph {
    public class MainViewModel {
        public const int MAX_DATA_SAMPLES = 960;
        public MainViewModel() {
            MyModel = new PlotModel {
                Title = "Acceleration",
                IsLegendVisible = true
            };
            MyModel.Series.Add(new LineSeries {
                BrokenLineStyle = LineStyle.Solid,
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "x-axis"
            });
            MyModel.Series.Add(new LineSeries {
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = "y-axis"
            });
            MyModel.Series.Add(new LineSeries {
                MarkerStroke = OxyColor.FromRgb(0, 0, 1),
                LineStyle = LineStyle.Solid,
                Title = "z-axis"
            });
            MyModel.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -8f,
                AbsoluteMaximum = 8f,
                Minimum = -8f,
                Maximum = 8f,
                Title = "Value"
            });
            MyModel.Axes.Add(new LinearAxis {
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });
        }

        public PlotModel MyModel { get; private set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LineGraph : Page {

        private IMetaWearBoard metawear;
        private IAccelerometer accelerometer;

        public LineGraph() {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            var samples = 0;
            var model = (DataContext as MainViewModel).MyModel;

            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
            accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Configure(odr: 100f, range: 8f);

            await accelerometer.PackedAcceleration.AddRouteAsync(source => source.Stream(async data => {
                var value = data.Value<Acceleration>();
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.X));
                    (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Y));
                    (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Z));
                    samples++;

                    model.InvalidatePlot(true);
                    if (samples > MainViewModel.MAX_DATA_SAMPLES) {
                        model.Axes[1].Reset();
                        model.Axes[1].Maximum = samples;
                        model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
                        model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
                    }
                });
            }));
        }

        private async void back_Click(object sender, RoutedEventArgs e) {
            if (!metawear.InMetaBootMode) {
                metawear.TearDown();
                await metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }

        private void streamSwitch_Toggled(object sender, RoutedEventArgs e) {
            if (streamSwitch.IsOn) {
                accelerometer.Acceleration.Start();
                accelerometer.Start();
            } else {
                accelerometer.Stop();
                accelerometer.Acceleration.Stop();
            }
        }
    }
}
