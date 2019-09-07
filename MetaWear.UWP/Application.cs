using MbientLab.MetaWear;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Impl.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace MetaWear.UWP
{
    /// <summary>
    /// Entry point into the MetaWear API for UWP or .NET console apps
    /// </summary>
    public class Application : IApplication
    {

        internal Dictionary<ulong, Tuple<MetaWearBoard, BluetoothLeGatt, LibraryIO>> BtleDevices = new Dictionary<ulong, Tuple<MetaWearBoard, BluetoothLeGatt, LibraryIO>>();
        internal static string CachePath = ".metawear";

        private IScanner _scanner;
        public IScanner Scanner => _scanner = _scanner ?? new Scanner();

        /// <summary>
        /// Set the path the API uses to cache data
        /// </summary>
        /// <param name="path">New path to use</param>
        public static void SetCacheFolder(string path)
        {
            CachePath = path;
        }

        /// <summary>
        /// Instantiates an <see cref="IMetaWearBoard"/> object corresponding to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device object corresponding to the target MetaWear board</param>
        /// <returns><see cref="IMetaWearBoard"/> object</returns>
        public IMetaWearBoard GetMetaWearBoard(IScannerResult result)
        {
            if (BtleDevices.TryGetValue(result.Address, out var value))
            {
                return value.Item1;
            }

            var device = (result as ScannerResult).Device;
            var gatt = new BluetoothLeGatt(device);
            var io = new LibraryIO(device.BluetoothAddress);
            value = Tuple.Create(new MetaWearBoard(gatt, io), gatt, io);
            BtleDevices.Add(device.BluetoothAddress, value);
            return value.Item1;
        }
        /// <summary>
        /// Removes the <see cref="IMetaWearBoard"/> object corresponding to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device object corresponding to the target MetaWear board</param>
        /// <param name="dispose">True if existing references to the BluetoothLEDevice object should be disposed of</param>
        public void RemoveMetaWearBoard(uint address, bool dispose = false)
        {
            if (BtleDevices.TryGetValue(address, out var value))
            {
                if (dispose)
                {
                    value.Item2.Close();
                }
                BtleDevices.Remove(address);
            }
        }

        public void RemoveMetaWearBoard(string address, bool dispose = false)
        {
            var stripped = address.Replace(":", "");
            if (UInt32.TryParse(stripped, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint asInt))
            {
                RemoveMetaWearBoard(asInt, dispose);
            }
        }

        /// <summary>
        /// Clears cached information specific to the BluetoothLE device
        /// </summary>
        /// <param name="device">BluetoothLE device to clear</param>
        /// <returns>Null task</returns>
        public async Task ClearDeviceCacheAsync(IScannerResult device)
        {
            var macAddr = device.Address.ToString("X");

#if WINDOWS_UWP
            var root = await ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(CachePath) != null) ?
                ApplicationData.Current.LocalFolder.GetFolderAsync(CachePath) :
                ApplicationData.Current.LocalFolder.CreateFolderAsync(CachePath));

            if (await root.TryGetItemAsync(macAddr) != null)
            {
                await (await root.GetFolderAsync(macAddr)).DeleteAsync();
            }
#else
            var path = Path.Combine(CachePath, macAddr);

            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
            await Task.CompletedTask;
#endif
        }
    }
}