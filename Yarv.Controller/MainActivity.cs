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
using Google.Android.Material.Slider;

namespace Yarv.Controller
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IBaseOnChangeListener
    {
        private const int SpeedCount = 5;
        private const int SectionCount = 3;
        private const int SegmentCount = SpeedCount * SectionCount;

        private aw.Button _buttonDisconnect;
        private aw.ListView _listViewAvailableDevices;
        private aw.LinearLayout _linearLayoutTouchpad;
        private aw.Button _buttonConnectCar;
        private aw.Button _buttonConnectBoat;
        private aw.TextView _textViewDebug;
        private aw.TextView _textViewNoBluetoothDevices;
        private BluetoothSocket _socket;
        private BluetoothDevice _deviceCar;
        private BluetoothDevice _deviceBoat;
        private bool _debugOptionsEnabled;
        private FloatingActionButton _fabCheckStatus;
        private View _contentConnect;
        private View _contentMain;
        private Slider _sliderSpeed;
        private aw.LinearLayout _linearLayoutControl;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            _contentConnect = FindViewById<View>(Resource.Id.contentConnect);
            _contentConnect.Show();

            _contentMain = FindViewById<View>(Resource.Id.contentMain);
            _contentMain.Hide();

            _textViewDebug = FindViewById<aw.TextView>(Resource.Id.textViewDebug);
            _textViewNoBluetoothDevices = FindViewById<aw.TextView>(Resource.Id.textViewNoBluetoothDevices);

            _fabCheckStatus = FindViewById<FloatingActionButton>(Resource.Id.fabCheckStatus);
            _fabCheckStatus.Hide();
            _fabCheckStatus.Click += FabCheckStatus_OnClick;

            _buttonConnectCar = FindViewById<aw.Button>(Resource.Id.buttonConnectCar);
            _buttonConnectCar.Click += ButtonConnectCar_Click;

            _buttonConnectBoat = FindViewById<aw.Button>(Resource.Id.buttonConnectBoat);
            _buttonConnectBoat.Click += ButtonConnectBoat_Click;

            _buttonDisconnect = FindViewById<aw.Button>(Resource.Id.buttonDisconnect);
            _buttonDisconnect.Hide();
            _buttonDisconnect.Click += ButtonDisconnect_Click;

            _listViewAvailableDevices = FindViewById<aw.ListView>(Resource.Id.listViewAvailableDevices);

            _linearLayoutTouchpad = FindViewById<aw.LinearLayout>(Resource.Id.linearLayoutTouchpad);
            _linearLayoutTouchpad.Show();
            _linearLayoutTouchpad.Touch += _linearLayoutTouchpad_Touch;

            _sliderSpeed = FindViewById<Slider>(Resource.Id.sliderSpeed);
            var method = Java.Lang.Class.ForName("com.google.android.material.slider.BaseSlider")
                .GetDeclaredMethods()
                .FirstOrDefault(x => x.Name == "addOnChangeListener");
            method?.Invoke(_sliderSpeed, this);

            _linearLayoutControl = FindViewById<aw.LinearLayout>(Resource.Id.linearLayoutControl);
            _linearLayoutControl.Hide();
            for (var i = 0; i < _linearLayoutControl.ChildCount; i++)
            {
                var row = _linearLayoutControl.GetChildAt(i) as aw.LinearLayout;
                if (row != null)
                {
                    for (var j = 0; j < row.ChildCount; j++)
                    {
                        var button = row.GetChildAt(j) as aw.Button;
                        if (button != null)
                        {
                            button.Touch += ButtonControl_Touch;
                        }
                    }
                }
            }

            InitializeDebugOptions(false);
            InitializeDevice();
        }

        public void OnValueChange(Java.Lang.Object p0, float value, bool p2)
        {
            SendCommand("set-speed", (int)value);
        }
        private void ButtonControl_Touch(object sender, View.TouchEventArgs e)
        {
            var view = (View)sender;
            switch (e.Event.Action)
            {
                case MotionEventActions.Down:
                    SendCommand(view.Tag.ToString());
                    break;
                case MotionEventActions.Up:
                    SendCommand("stop");
                    break;
            }
            
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
                if (point.Y >= 5 && point.Y <= 10)
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
            _deviceCar = (from bd in adapter.BondedDevices
                       where bd.Name == "YarvCar"
                       select bd).FirstOrDefault();
            _deviceBoat = (from bd in adapter.BondedDevices
                          where bd.Name == "YarvBoat"
                          select bd).FirstOrDefault();

            if (_deviceCar == null)
            {
                _buttonConnectCar.Hide();
            }
            else
            {
                _buttonConnectCar.Show();
            }

            if (_deviceBoat == null)
            {
                _buttonConnectBoat.Hide();
            }
            else
            {
                _buttonConnectBoat.Show();
            }

            if (adapter.BondedDevices.Any())
            {
                _textViewNoBluetoothDevices.Hide();
                _listViewAvailableDevices.Show();
                var list = adapter.BondedDevices.Select(x => x.Name).ToList();
                _listViewAvailableDevices.Adapter = new aw.ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, list);
            }
            else
            {
                _textViewNoBluetoothDevices.Show();
                _listViewAvailableDevices.Hide();
            }
        }

        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            _socket.Close();
            InitializeDevice();
            _fabCheckStatus.Hide();
            _contentMain.Hide();
            _contentConnect.Show();
        }

        private void ButtonConnectCar_Click(object sender, EventArgs e)
        {
            Connect((View)sender, _deviceCar);
        }

        private void ButtonConnectBoat_Click(object sender, EventArgs e)
        {
            Connect((View)sender, _deviceBoat);
        }

        private void Connect(View sender, BluetoothDevice device)
        {
            _socket = device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            try
            {
                _socket.Connect();

                _buttonConnectCar.Hide();
                _buttonConnectBoat.Hide();
                _listViewAvailableDevices.Hide();
                _buttonDisconnect.Show();

                _contentConnect.Hide();
                _contentMain.Show();

                _fabCheckStatus.Show();
            }
            catch (Exception)
            {
                var view = sender;
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

            var touchpadMenuItem = menu.FindItem(Resource.Id.action_touchpad);
            var controlMenuItem = menu.FindItem(Resource.Id.action_control);

            if (_contentMain.Visibility == ViewStates.Visible)
            {
                debugMenuItem.SetVisible(true);
                touchpadMenuItem.SetVisible(_linearLayoutTouchpad.Visibility == ViewStates.Gone);
                controlMenuItem.SetVisible(_linearLayoutControl.Visibility == ViewStates.Gone);
            }
            else
            {
                debugMenuItem.SetVisible(false);
                touchpadMenuItem.SetVisible(false);
                controlMenuItem.SetVisible(false);
            }

            var connectMenuItem = menu.FindItem(Resource.Id.action_connect);
            connectMenuItem.SetVisible(_contentConnect.Visibility == ViewStates.Gone);

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
                case Resource.Id.action_touchpad:
                    ShowTouchpad();
                    return true;
                case Resource.Id.action_control:
                    ShowControl();
                    return true;
                case Resource.Id.action_simulate:
                    InitializeSimulation();
                    return true;
                case Resource.Id.action_connect:
                    ShowConnection();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ShowControl()
        {
            _linearLayoutTouchpad.Hide();
            _linearLayoutControl.Show();
        }

        private void ShowTouchpad()
        {
            _linearLayoutTouchpad.Show();
            _linearLayoutControl.Hide();
        }

        private void ShowConnection()
        {
            _contentMain.Hide();
            _contentConnect.Show();
        }

        private void InitializeSimulation()
        {
            InitializeDebugOptions(true);
            _contentMain.Show();
            _contentConnect.Hide();
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
