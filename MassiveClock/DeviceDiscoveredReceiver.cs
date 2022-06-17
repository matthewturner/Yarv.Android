using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MassiveClock
{
    class DeviceDiscoveredReceiver : BroadcastReceiver
    {
        private Activity _mainActivity;
        private readonly List<string> _deviceList = new List<string>();

        public DeviceDiscoveredReceiver(Activity activity)
        {
            _mainActivity = activity;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (!BluetoothDevice.ActionFound.Equals(intent.Action))
            {
                return;
            }

            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
            _deviceList.Add(device.Name + ";" + device.Address);

            //var path = Android.OS.Environment.ExternalStorageDirectory + Java.IO.File.Separator + "Download";
            //string filename = Path.Combine(path, "myfile.txt");
            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}
            //using (StreamWriter objStreamWriter = new StreamWriter(filename, true))
            //{
            //    objStreamWriter.WriteLine(deviceList.Last());
            //    objStreamWriter.Close();
            //}
        }
    }
}