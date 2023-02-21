using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Bluetooth;
using Java.Util;
using System.Text;
using System.Linq;
using aw = Android.Widget;
using Android.Graphics;
using Google.Android.Material.Slider;

namespace Yarv.Controller
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IBaseOnChangeListener
    {
        private const int LeftBoundary = 30;
        private const int RightBoundary = 70;
        private const int Forward5Boundary = 15;
        private const int Forward4Boundary = 20;
        private const int Forward3Boundary = 25;
        private const int Forward2Boundary = 30;
        private const int Forward1Boundary = 35;
        private const int ForwardStopBoundary = 35;

        private const int ReverseStopBoundary = 65;
        private const int Reverse1Boundary = 70;
        private const int Reverse2Boundary = 75;
        private const int Reverse3Boundary = 80;
        private const int Reverse4Boundary = 85;
        private const int Reverse5Boundary = 100;

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
        private string _lastCommand;
        private bool _autoPilotEnabled;

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
            _linearLayoutTouchpad.Touch += LinearLayoutTouchpad_Touch;

            _sliderSpeed = FindViewById<Slider>(Resource.Id.sliderSpeed);
            var method = Java.Lang.Class.ForName("com.google.android.material.slider.BaseSlider")
                .GetDeclaredMethods()
                .FirstOrDefault(x => x.Name == "addOnChangeListener");
            method?.Invoke(_sliderSpeed, this);

            _linearLayoutControl = FindViewById<aw.LinearLayout>(Resource.Id.linearLayoutControl);
            _linearLayoutControl.Hide();
            AttachButtonHandlers();

            InitializeDebugOptions(false);
            InitializeDevice();
        }

        private void AttachButtonHandlers()
        {
            for (var i = 0; i < _linearLayoutControl.ChildCount; i++)
            {
                if (_linearLayoutControl.GetChildAt(i) is aw.LinearLayout row)
                {
                    for (var j = 0; j < row.ChildCount; j++)
                    {
                        if (row.GetChildAt(j) is aw.Button button)
                        {
                            button.Touch += ButtonControl_Touch;
                        }
                    }
                }
            }
        }

        public void OnValueChange(Java.Lang.Object p0, float value, bool p2)
        {
            SendCommand(Commands.SetSpeed, (int)value);
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
                    SendCommand(Commands.Stop);
                    break;
            }
            
        }

        private void LinearLayoutTouchpad_Touch(object sender, View.TouchEventArgs e)
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
                    SendCommand(Commands.Stop);
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
          
            var currentCommand = encodedCommand.ToString();

            if (currentCommand == _lastCommand)
            {
                _textViewDebug.Text = currentCommand;
                return;
            }

            _lastCommand = currentCommand;
            _textViewDebug.Text = currentCommand + " (new)";
            if (_socket != null)
            {
                var buffer = Encoding.UTF8.GetBytes(currentCommand);
                _socket.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        private Point ApproxCoordinateFor(MotionEvent ev)
        {
            var x = ev.GetX();
            var y = ev.GetY();

            var xCoord = (int)Map(x, 0, _linearLayoutTouchpad.Width, 0, 100, 1);
            var yCoord = (int)Map(y, 0, _linearLayoutTouchpad.Height, 0, 100, 1);

            return new Point(xCoord, yCoord);
        }

        private void SendCommandsFor(MotionEvent ev)
        {
            var point = ApproxCoordinateFor(ev);
            if (point.Y < 0)
            {
                return;
            }
            if (point.Y > 100)
            {
                return;
            }

            if (point.X < LeftBoundary)
            {
                if (point.Y < ForwardStopBoundary)
                {
                    SendCommand(Commands.BearLeftForward);
                    return;
                }
                if (point.Y > ReverseStopBoundary)
                {
                    SendCommand(Commands.BearLeftReverse);
                    return;
                }

                SendCommand(Commands.SetSpeed, 5, Commands.Left);
                return;
            }

            if (point.X > RightBoundary)
            {
                if (point.Y < ForwardStopBoundary)
                {
                    SendCommand(Commands.BearRightFoward);
                    return;
                }
                if (point.Y > ReverseStopBoundary)
                {
                    SendCommand(Commands.BearRightReverse);
                    return;
                }

                SendCommand(Commands.SetSpeed, 5, Commands.Right);
                return;
            }

            if (point.Y < Forward5Boundary)
            {
                SendCommand(Commands.SetSpeed, 5, Commands.Forward);
                return;
            }

            if (point.Y < Forward4Boundary)
            {
                SendCommand(Commands.SetSpeed, 4, Commands.Forward);
                return;
            }

            if (point.Y < Forward3Boundary)
            {
                SendCommand(Commands.SetSpeed, 3, Commands.Forward);
                return;
            }

            if (point.Y < Forward2Boundary)
            {
                SendCommand(Commands.SetSpeed, 2, Commands.Forward);
                return;
            }

            if (point.Y < Forward1Boundary)
            {
                SendCommand(Commands.SetSpeed, 1, Commands.Forward);
                return;
            }

            if (point.Y < ReverseStopBoundary)
            {
                SendCommand(Commands.Stop);
                return;
            }

            if (point.Y < Reverse1Boundary)
            {
                SendCommand(Commands.SetSpeed, 1, Commands.Reverse);
                return;
            }

            if (point.Y < Reverse2Boundary)
            {
                SendCommand(Commands.SetSpeed, 2, Commands.Reverse);
                return;
            }

            if (point.Y < Reverse3Boundary)
            {
                SendCommand(Commands.SetSpeed, 3, Commands.Reverse);
                return;
            }

            if (point.Y < Reverse4Boundary)
            {
                SendCommand(Commands.SetSpeed, 4, Commands.Reverse);
                return;
            }

            if (point.Y < Reverse5Boundary)
            {
                SendCommand(Commands.SetSpeed, 5, Commands.Reverse);
                return;
            }

            return;
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

        private bool IsConnected
        {
            get
            {
                if (_socket != null && _socket.IsConnected)
                {
                    return true;
                }
                return false;
            }
        }

        private void InitializeDevice()
        {
            if (IsConnected)
            {
                _buttonConnectCar.Hide();
                _buttonConnectBoat.Hide();
                _listViewAvailableDevices.Hide();
                _textViewNoBluetoothDevices.Hide();
                _buttonDisconnect.Show();
            }
            else
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                _deviceCar = (from bd in adapter.BondedDevices
                       where bd.Name == "Yarv-Car"
                       select bd).FirstOrDefault();
                _deviceBoat = (from bd in adapter.BondedDevices
                          where bd.Name == "Yarv-Boat"
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

            var autoPilotMenuItem = menu.FindItem(Resource.Id.action_auto_pilot);
            autoPilotMenuItem.SetChecked(_autoPilotEnabled);

            var touchpadMenuItem = menu.FindItem(Resource.Id.action_touchpad);
            var controlMenuItem = menu.FindItem(Resource.Id.action_control);

            if (_contentMain.Visibility == ViewStates.Visible)
            {
                debugMenuItem.SetVisible(true);
                autoPilotMenuItem.SetVisible(true);
                touchpadMenuItem.SetVisible(_linearLayoutTouchpad.Visibility == ViewStates.Gone);
                controlMenuItem.SetVisible(_linearLayoutControl.Visibility == ViewStates.Gone);
            }
            else
            {
                debugMenuItem.SetVisible(false);
                autoPilotMenuItem.SetVisible(false);
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
                case Resource.Id.action_auto_pilot:
                    SetAutoPilot(!_autoPilotEnabled);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void SetAutoPilot(bool autoPilotEnabled)
        {
            _autoPilotEnabled = autoPilotEnabled;

            if (_autoPilotEnabled)
            {
                SendCommand(Commands.EnableAutoPilot);
            }
            else
            {
                SendCommand(Commands.DisableAutoPilot);
            }
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
