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
using System.Threading;
using Newtonsoft.Json;
using aw = Android.Widget;
using Android.Graphics;

namespace Yarv.Controller
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private aw.Button _buttonDisconnect;
        private aw.ListView _listViewAvailableDevices;
        private aw.LinearLayout _linearLayoutTouchpad;
        private aw.Button _buttonConnect;
        private aw.TextView _textViewDebug;
        private BluetoothSocket _socket;
        private BluetoothDevice _device;
        private bool _debugOptionsEnabled;
        private FloatingActionButton _fabCheckStatus;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            _textViewDebug = FindViewById<aw.TextView>(Resource.Id.textViewDebug);
            _textViewDebug.Hide();

            _fabCheckStatus = FindViewById<FloatingActionButton>(Resource.Id.fabCheckStatus);
            _fabCheckStatus.Hide();
            _fabCheckStatus.Click += FabCheckStatus_OnClick;

            _buttonConnect = FindViewById<aw.Button>(Resource.Id.buttonConnect);
            _buttonConnect.Click += ButtonConnect_Click;

            _buttonDisconnect = FindViewById<aw.Button>(Resource.Id.buttonDisconnect);
            _buttonDisconnect.Hide();
            _buttonDisconnect.Click += ButtonDisconnect_Click;

            _listViewAvailableDevices = FindViewById<aw.ListView>(Resource.Id.listViewAvailableDevices);

            _linearLayoutTouchpad = FindViewById<aw.LinearLayout>(Resource.Id.linearLayoutTouchpad);
            _linearLayoutTouchpad.Touch += _linearLayoutControl_Touch;
            // _linearLayoutTouchpad.Hide();

            InitializeDebugOptions(false);
            InitializeDevice();
        }

        private void _linearLayoutControl_Touch(object sender, View.TouchEventArgs e)
        {
            switch (e.Event.Action)
            {
                case MotionEventActions.Down:
                    var command = CommandFor(e.Event);
                    SendCommand(command);
                    break;
                case MotionEventActions.Move:
                    break;
                case MotionEventActions.Up:
                    SendCommand("stop");
                    break;
            }
        }

        private void SendCommand(string command, int data)
        {
            SendCommand($"{command}:{data}");
        }

        private void SendCommand(string command)
        {
            var encodedCommand = $">{command}!";
            _textViewDebug.Text = encodedCommand;
            if (_socket != null)
            {
                var buffer = Encoding.UTF8.GetBytes(encodedCommand);
                _socket.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        private string CommandFor(MotionEvent ev)
        {
            var point = new Point((int)ev.GetX(), (int)ev.GetY());
            return "forward";
        }

        private void InitializeDevice()
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            _device = (from bd in adapter.BondedDevices
                       where bd.Name == "YarvCar"
                       select bd).FirstOrDefault();

            if (_device == null)
            {
                InitializeDebugOptions(true);
                // _buttonConnect.Hide();
                // _listViewAvailableDevices.Show();
                // var list = adapter.BondedDevices.Select(x => x.Name).ToList();
                // _listViewAvailableDevices.Adapter = new aw.ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, list);
            }
            else
            {
                _buttonConnect.Show();
                _listViewAvailableDevices.Hide();
            }
        }

        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            _socket.Close();
            _buttonConnect.Show();
            _buttonDisconnect.Hide();
            _fabCheckStatus.Hide();
            _linearLayoutTouchpad.Hide();
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            try
            {
                _socket.Connect();

                _buttonConnect.Hide();
                _buttonDisconnect.Show();
                _linearLayoutTouchpad.Show();
                _fabCheckStatus.Show();
            }
            catch(Exception)
            {
                var view = (View)sender;
                Snackbar.Make(view, "Unable to connect. Are you in range?", Snackbar.LengthLong)
                    .SetAction("Action", (View.IOnClickListener)null).Show();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var debugMenuItem = menu.FindItem(Resource.Id.action_debug);
            debugMenuItem.SetChecked(_debugOptionsEnabled);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_settings:
                    return true;
                case Resource.Id.action_debug:
                    InitializeDebugOptions(!_debugOptionsEnabled);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void InitializeDebugOptions(bool debugOptionsEnabled)
        {
            _debugOptionsEnabled = debugOptionsEnabled;

            if(_debugOptionsEnabled )
            {
                _textViewDebug.Show();
            }
            else
            {
                _textViewDebug.Hide();
            }
        }

        private void FabCheckStatus_OnClick(object sender, EventArgs eventArgs)
        {
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
