using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.Content;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Content;
using Android.Bluetooth;
using System.Collections.Generic;
using Java.Util;
using System.Text;
using System.Linq;

namespace MassiveClock
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Android.Widget.TextView _textviewRawStatus;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            var buttonConnect = FindViewById<Android.Widget.Button>(Resource.Id.buttonConnect);
            buttonConnect.Click += ButtonConnect_Click;

            _textviewRawStatus = FindViewById<Android.Widget.TextView>(Resource.Id.rawStatus);
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            BluetoothDevice device = (from bd in adapter.BondedDevices
                                      where bd.Name == "MassiveClock"
                                      select bd).FirstOrDefault();

            if (device == null)
            {
                //throw new Exception("Named device not found");
                var view = (View)sender;
                Snackbar.Make(view, "Named device not found", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }

            var socket = device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            socket.Connect();

            var buffer = Encoding.UTF8.GetBytes(">status!");
            socket.OutputStream.Write(buffer, 0, buffer.Length);

            var statusBuffer = new byte[1024];
            socket.InputStream.Read(statusBuffer, 0, statusBuffer.Length);

            var status = Encoding.UTF8.GetString(statusBuffer);

            _textviewRawStatus.SetText(status, Android.Widget.TextView.BufferType.Normal);
            socket.Close();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            var view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
