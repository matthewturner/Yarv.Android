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
        private const int SpeedCount = 5;
        private const int SectionCount = 3;
        private const int SegmentCount = SpeedCount * SectionCount;

        private aw.Button _buttonDisconnect;
        private aw.ListView _listViewAvailableDevices;
        private aw.LinearLayout _linearLayoutTouchpad;
        private aw.Button _buttonConnect;
        private aw.TextView _textViewDebug;
        private BluetoothSocket _socket;
        private BluetoothDevice _device;
        private bool _debugOptionsEnabled;
        private bool _simulationEnabled;
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
            _linearLayoutTouchpad.Touch += _linearLayoutTouchpad_Touch;
            _linearLayoutTouchpad.Hide();

            InitializeDebugOptions(false);
            InitializeDevice();
        }

        private void _linearLayoutTouchpad_Touch(object sender, View.TouchEventArgs e)
        {
            switch (e.Event.Action)
            {
                case MotionEventActions.Down:
                    SendCommandsFor(e.Event);
                    break;
                case MotionEventActions.Move:
                    SendCommandsFor(e.Event);
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

        private void SendCommand(string command, int data, string followupCommand)
        {
            SendCommand($"{command}:{data}", followupCommand);
        }

        private void SendCommand(params string[] commands)
        {
            var encodedCommand = new StringBuilder();
            foreach(var command in commands)
            {
                encodedCommand.Append($">{command}!");
            }
          
            _textViewDebug.Text = encodedCommand.ToString();
            if (_socket != null)
            {
                var buffer = Encoding.UTF8.GetBytes(encodedCommand.ToString());
                _socket.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        private Point ApproxCoordinateFor(MotionEvent ev)
        {
            var x = ev.GetX();
            var y = ev.GetY();

            var xCoord = (int)Map(x, 0, _linearLayoutTouchpad.Width, 0, SegmentCount, 1);
            var yCoord = (int)Map(y, 0, _linearLayoutTouchpad.Height, 0, SegmentCount, 1);

            return new Point(xCoord, yCoord);
        }

        private void SendCommandsFor(MotionEvent ev)
        {
            var point = ApproxCoordinateFor(ev);
            if (point.Y < 0)
            {
                return;
            }
            if (point.Y > 15)
            {
                return;
            }

            if (point.X >= 5 && point.X < 10)
            {
                if (point.Y >= 5 && point.Y < 10)
                {
                    SendCommand("stop");
                    return;
                }
            }

            var speed = CoordinateToSpeed(point.Y);

            if (point.X < 5)
            {
                if (speed > 0)
                {
                    SendCommand("bear-left-forward");
                    return;
                }
                if (speed < 0)
                {
                    SendCommand("bear-left-reverse");
                    return;
                }

                SendCommand("left");
                return;
            }

            if (point.X >= 5 && point.X < 10)
            {    
                if (speed > 0)
                {
                    SendCommand("set-speed", speed, "forward");
                    return;
                }

                SendCommand("set-speed", Math.Abs(speed), "reverse");
                return;
            }

            if (point.X < 15)
            {
                if (speed > 0)
                {
                    SendCommand("bear-right-forward");
                    return;
                }
                if (speed < 0)
                {
                    SendCommand("bear-right-reverse");
                    return;
                }

                SendCommand("right");
                return;
            }
        }

        private static int CoordinateToSpeed(int y)
        {
            if (y < 5)
            {
                return 5 - y;
            }
            if (y < 10)
            {
                return 0;
            }    
            if (y <= 15)
            {
                return -(y - 10);
            }
            return 0;
        }

        private static double Map(float sourceNumber, float fromA, float fromB, float toA, float toB, int decimalPrecision)
        {
            float deltaA = fromB - fromA;
            float deltaB = toB - toA;
            float scale = deltaB / deltaA;
            float negA = -1 * fromA;
            float offset = (negA * scale) + toA;
            float finalNumber = (sourceNumber * scale) + offset;
            int calcScale = (int)Math.Pow(10, decimalPrecision);
            return (float)Math.Round(finalNumber * calcScale) / calcScale;
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
                _buttonConnect.Hide();
                _listViewAvailableDevices.Show();
                var list = adapter.BondedDevices.Select(x => x.Name).ToList();
                _listViewAvailableDevices.Adapter = new aw.ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, list);
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

            var simulateMenuItem = menu.FindItem(Resource.Id.action_simulate);
            simulateMenuItem.SetChecked(_simulationEnabled);

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_settings:
                    return true;
                case Resource.Id.action_increase_edge_duration:
                    SendCommand("increase-edge-duration");
                    return true;
                case Resource.Id.action_decrease_edge_duration:
                    SendCommand("decrease-edge-duration");
                    return true;
                case Resource.Id.action_debug:
                    InitializeDebugOptions(!_debugOptionsEnabled);
                    return true;
                case Resource.Id.action_simulate:
                    InitializeSimulateOptions(!_simulationEnabled);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void InitializeSimulateOptions(bool simulateEnabled)
        {
            _simulationEnabled = simulateEnabled;

            if (_simulationEnabled)
            {
                _linearLayoutTouchpad.Show();
                _buttonConnect.Hide();
                _buttonDisconnect.Hide();
                _listViewAvailableDevices.Hide();
            }
            else
            {
                _linearLayoutTouchpad.Hide();
                _buttonConnect.Hide();
                _buttonDisconnect.Hide();
                _listViewAvailableDevices.Show();
            }
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
