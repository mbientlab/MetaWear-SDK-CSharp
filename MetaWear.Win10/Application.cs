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
            private string macAddr;

            public IO(ulong macAddr) {
                this.macAddr = macAddr.ToString("X");
            }

#if WINDOWS_UWP
            public async Task<Stream> LocalLoadAsync(string key) {
                StorageFolder root, folder;

                root = await ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(cachePath) != null) ?
                    ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath) :
                    ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath));
                folder = await (await root.TryGetItemAsync(macAddr) == null ? root.CreateFolderAsync(macAddr) : root.GetFolderAsync(macAddr));

                return await folder.OpenStreamForReadAsync(string.Format("{0}.bin", key));
            }

            public async Task LocalSaveAsync(string key, byte[] data) {
                StorageFolder root, folder;

                root = await ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(cachePath) != null) ?
                    ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath) :
                    ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath));
                folder = await (await root.TryGetItemAsync(macAddr) == null ? root.CreateFolderAsync(macAddr) : root.GetFolderAsync(macAddr));

                using (var stream = await folder.OpenStreamForWriteAsync(string.Format("{0}.bin", key), CreationCollisionOption.ReplaceExisting)) {
                    stream.Write(data, 0, data.Length);
                }
            }
#else
            public async Task<Stream> LocalLoadAsync(string key) {
                return await Task.FromResult(File.Open(Path.Combine(Directory.GetCurrentDirectory(), cachePath, macAddr, key), FileMode.Open));
            }

            public Task LocalSaveAsync(string key, byte[] data) {
                var root = Path.Combine(Directory.GetCurrentDirectory(), cachePath, macAddr);
                if (!Directory.Exists(root)) {
                    Directory.CreateDirectory(root);
                }
                using (Stream outs = File.Open(Path.Combine(root, key), FileMode.Create)) {
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
        /// <summary>
        /// Removes the <see cref="IMetaWearBoard"/> object corresponding to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device object corresponding to the target MetaWear board</param>
        public static void RemoveMetaWearBoard(BluetoothLEDevice device) {
            btleDevices.Remove(device.BluetoothAddress);
        }
        /// <summary>
        /// Clears cached information specific to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device to clear</param>
        /// <returns>Null task</returns>
        public static async Task ClearDeviceCacheAsync(BluetoothLEDevice device) {
#if WINDOWS_UWP
            var macAddr = device.BluetoothAddress.ToString("X");
            var root = await((await ApplicationData.Current.LocalFolder.TryGetItemAsync(cachePath) != null) ?
                ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath) :
                ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath));

            if (await root.TryGetItemAsync(macAddr) != null) {
                await (await root.GetFolderAsync(macAddr)).DeleteAsync();
            }
#else
            var macAddr = device.BluetoothAddress.ToString("X");
            var path = Path.Combine(cachePath, macAddr);

            File.Delete(path);
            await Task.CompletedTask;
#endif
        }
    }
}
