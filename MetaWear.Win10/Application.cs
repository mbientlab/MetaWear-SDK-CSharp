using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace MbientLab.MetaWear.Win10 {
    /// <summary>
    /// Entry point into the MetaWear API for UWP or .NET console apps
    /// </summary>
    public class Application {
        private class IO : ILibraryIO {
            private ulong macAddr;

            public IO(ulong macAddr) {
                this.macAddr = macAddr;
            }

#if WINDOWS_UWP
            public async Task<Stream> LocalLoadAsync(string key) {
                StorageFolder folder;
                try {
                    folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath);
                } catch (FileNotFoundException) {
                    folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath);
                }
                return await folder.OpenStreamForReadAsync(string.Format("{0:X}_{1}.bin", macAddr, key));
            }

            public void LogWarn(string tag, string message, Exception e) {
                System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1}\r\n{2}", tag, message, e.StackTrace));
            }

            public async Task LocalSaveAsync(string key, byte[] data) {
                StorageFolder folder;
                try {
                    folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath);
                } catch (FileNotFoundException) {
                    folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath);
                }
                using (var stream = await folder.OpenStreamForWriteAsync(string.Format("{0:X}_{1}.bin", macAddr, key), CreationCollisionOption.ReplaceExisting)) {
                    stream.Write(data, 0, data.Length);
                }
            }
#else
            public async Task<Stream> LocalLoadAsync(string key) {
                return await Task.FromResult(File.Open(Path.Combine(cachePath, macAddr.ToString("X"), key), FileMode.Open));
            }

            public void LogWarn(string tag, string message, Exception e) {
                Console.WriteLine(string.Format("{0}: {1}\r\n{2}", tag, message, e.StackTrace));
            }

            public Task LocalSaveAsync(string key, byte[] data) {
                using (Stream outs = File.Open(Path.Combine(cachePath, macAddr.ToString("X"), key), FileMode.Create)) {
                    outs.Write(data, 0, data.Length);
                }
                return Task.CompletedTask;
            }
#endif
        }

        private static Dictionary<ulong, MetaWearBoard> btleDevices = new Dictionary<ulong, MetaWearBoard>();
        private static string cachePath = ".metawear";

        /// <summary>
        /// Set the path the API uses to cache data
        /// </summary>
        /// <param name="path">New path to use</param>
        public static void SetCacheFolder(string path) {
            cachePath = path;
        }

        /// <summary>
        /// Instantiates an <see cref="IMetaWearBoard"/> object corresponding to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device object corresponding to the target MetaWear board</param>
        /// <returns><see cref="IMetaWearBoard"/> object</returns>
        public static IMetaWearBoard GetMetaWearBoard(BluetoothLEDevice device) {
            if (btleDevices.TryGetValue(device.BluetoothAddress, out var board)) {
                return board;
            }

            board = new MetaWearBoard(new BluetoothLeGatt(device), new IO(device.BluetoothAddress));
            btleDevices.Add(device.BluetoothAddress, board);
            return board;
        }
    }
}
