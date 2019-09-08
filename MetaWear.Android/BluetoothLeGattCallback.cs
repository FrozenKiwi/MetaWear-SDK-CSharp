using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;

namespace MetaWear.Android
{
    partial class BluetoothLeGatt
    {
        class BluetoothLeGattCallback : BluetoothGattCallback
        {
            private readonly BluetoothLeGatt platform;
            internal BluetoothLeGattCallback(BluetoothLeGatt owner)
            {
                platform = owner;
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
            {
                //final AndroidPlatform platform = btleDevices.get(gatt.getDevice());

                switch (newState)
                {
                    case ProfileState.Connected:
                        if (status != 0)
                        {
                            platform.CloseGatt();
                            platform.connectTask.SetError(
                                new IllegalStateException(string.Format("Non-zero onConnectionStateChange status (%s)", status))
                            );
                        }
                        else
                        {
                            Task.Delay(1000)
                                .ContinueWith(ignored => gatt.DiscoverServices());
                        }
                        break;
                    case ProfileState.Disconnected:
                        platform.Disconnected(status);
                        break;
                }
            }

            override public void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
            {
                if (status != 0)
                {
                    platform.CloseGatt();
                    platform.connectTask.SetError(new IllegalStateException(string.Format("Non-zero onServicesDiscovered status (%d)", status)));
                }
                else
                {
                    platform.connectTask.SetResult(true);
                }
            }

            override public void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                if (status != 0)
                {
                    platform.gattOpTask.SetError(new IllegalStateException(string.Format("Non-zero onCharacteristicRead status (%d)", status)));
                }
                else
                {
                    platform.gattOpTask.SetResult(characteristic.GetValue());
                }
            }

            override public void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                if (status != 0)
                {
                    platform.gattOpTask.SetError(new IllegalStateException(string.Format("Non-zero onCharacteristicWrite status (%d)", status)));
                }
                else
                {
                    platform.gattOpTask.SetResult(characteristic.GetValue());
                }
            }

            override public void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            {
                platform.notificationListener(characteristic.GetValue());
            }

            override public void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
            {
                if (status != 0)
                {
                    platform.gattOpTask.SetError(new IllegalStateException(string.Format("Non-zero onDescriptorWrite status (%d)", status)));
                }
                else
                {
                    platform.gattOpTask.SetResult(null);
                }
            }

            override public void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, GattStatus status)
            {
                if (status != 0)
                {
                    platform.gattOpTask.SetError(new IllegalStateException(string.Format("Non-zero onReadRemoteRssi status (%d)", status)));
                }
                else
                {
                    // TODO: validate endian-ness of this output
                    var r = BitConverter.GetBytes(rssi);
                    //platform.gattOpTask.SetResult(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(rssi).array());
                    platform.gattOpTask.SetResult(r);
                }
            }

        };
    }
}