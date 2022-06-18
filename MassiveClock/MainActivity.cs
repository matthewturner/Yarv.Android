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
        private Android.Widget.Button _buttonDisconnect;
        private Android.Widget.TextView _textViewRawStatus;
        private Android.Widget.ListView _listViewAvailableDevices;
        private Android.Widget.Button _buttonConnect;
        private BluetoothSocket _socket;
        private BluetoothDevice _device;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            _buttonConnect = FindViewById<Android.Widget.Button>(Resource.Id.buttonConnect);
            _buttonConnect.Click += ButtonConnect_Click;

            _buttonDisconnect = FindViewById<Android.Widget.Button>(Resource.Id.buttonDisconnect);
            _buttonDisconnect.Click += ButtonDisconnect_Click;

            _textViewRawStatus = FindViewById<Android.Widget.TextView>(Resource.Id.rawStatus);

            _listViewAvailableDevices = FindViewById<Android.Widget.ListView>(Resource.Id.listViewAvailableDevices);

            InitializeDevice();
        }

        private void InitializeDevice()
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            _device = (from bd in adapter.BondedDevices
                          where bd.Name == "MassiveClock"
                          select bd).FirstOrDefault();

            if (_device == null)
            {
                _textViewRawStatus.Text = "Named device not found";
                _buttonConnect.Visibility = ViewStates.Gone;
                _listViewAvailableDevices.Visibility = ViewStates.Visible;
                var list = adapter.BondedDevices.Select(x => x.Name).ToList();
                _listViewAvailableDevices.Adapter = new Android.Widget.ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, list);
            }
            else
            {
                _buttonConnect.Visibility = ViewStates.Visible;
                _listViewAvailableDevices.Visibility = ViewStates.Gone;
            }
        }

        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            _socket.Close();
            _buttonConnect.Visibility = ViewStates.Visible;
            _buttonDisconnect.Visibility = ViewStates.Gone;
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            _socket.Connect();

            var buffer = Encoding.UTF8.GetBytes(">status!");
            _socket.OutputStream.Write(buffer, 0, buffer.Length);

            var statusBuffer = new byte[1024];
            _socket.InputStream.Read(statusBuffer, 0, statusBuffer.Length);

            var status = Encoding.UTF8.GetString(statusBuffer);

            _textViewRawStatus.SetText(status, Android.Widget.TextView.BufferType.Normal);

            _buttonConnect.Visibility = ViewStates.Gone;
            _buttonDisconnect.Visibility = ViewStates.Visible;
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
