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

namespace MassiveClock
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private aw.Button _buttonDisconnect;
        private aw.TextView _textViewRawStatus;
        private aw.TextView _textViewPhoneUnixTime;
        private aw.TextView _textViewPhoneTime;
        private aw.TextView _textViewClockTime;
        private aw.TextView _textViewClockUnixTime;
        private aw.ListView _listViewAvailableDevices;
        private aw.Button _buttonConnect;
        private BluetoothSocket _socket;
        private BluetoothDevice _device;
        private aw.Button _buttonSimulate;
        private aw.TextView _textViewPhoneDate;
        private aw.TextView _textViewClockDate;
        private aw.ImageView _imageViewUnixTimeDifference;
        private aw.ImageView _imageViewTimeDifference;
        private aw.ImageView _imageViewDateDifference;
        private bool _debugOptionsEnabled;
        private aw.Button _buttonSynchronize;
        private FloatingActionButton _fabCheckStatus;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            _fabCheckStatus = FindViewById<FloatingActionButton>(Resource.Id.fabCheckStatus);
            _fabCheckStatus.Visibility = ViewStates.Gone;
            _fabCheckStatus.Click += FabCheckStatus_OnClick;

            _buttonConnect = FindViewById<aw.Button>(Resource.Id.buttonConnect);
            _buttonConnect.Click += ButtonConnect_Click;

            _buttonDisconnect = FindViewById<aw.Button>(Resource.Id.buttonDisconnect);
            _buttonDisconnect.Visibility = ViewStates.Gone;
            _buttonDisconnect.Click += ButtonDisconnect_Click;

            _buttonSimulate = FindViewById<aw.Button>(Resource.Id.buttonSimulate);
            _buttonSimulate.Visibility = ViewStates.Gone;
            _buttonSimulate.Click += ButtonSimulate_Click;

            _buttonSynchronize = FindViewById<aw.Button>(Resource.Id.buttonSynchronize);
            _buttonSynchronize.Visibility = ViewStates.Gone;
            _buttonSynchronize.Click += ButtonSynchronize_Click;

            _textViewRawStatus = FindViewById<aw.TextView>(Resource.Id.rawStatus);

            _textViewPhoneUnixTime = FindViewById<aw.TextView>(Resource.Id.textViewPhoneUnixTime);
            _textViewClockUnixTime = FindViewById<aw.TextView>(Resource.Id.textViewClockUnixTime);
            _textViewPhoneTime = FindViewById<aw.TextView>(Resource.Id.textViewPhoneTime);
            _textViewClockTime = FindViewById<aw.TextView>(Resource.Id.textViewClockTime);
            _textViewPhoneDate = FindViewById<aw.TextView>(Resource.Id.textViewPhoneDate);
            _textViewClockDate = FindViewById<aw.TextView>(Resource.Id.textViewClockDate);

            _imageViewUnixTimeDifference = FindViewById<aw.ImageView>(Resource.Id.imageViewUnixTimeDifference);
            _imageViewTimeDifference = FindViewById<aw.ImageView>(Resource.Id.imageViewTimeDifference);
            _imageViewDateDifference = FindViewById<aw.ImageView>(Resource.Id.imageViewDateDifference);

            _listViewAvailableDevices = FindViewById<aw.ListView>(Resource.Id.listViewAvailableDevices);

            InitializeDebugOptions(false);
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
                InitializeDebugOptions(true);
                _buttonConnect.Visibility = ViewStates.Gone;
                _listViewAvailableDevices.Visibility = ViewStates.Visible;
                var list = adapter.BondedDevices.Select(x => x.Name).ToList();
                _listViewAvailableDevices.Adapter = new aw.ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, list);
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
            _buttonSynchronize.Visibility = ViewStates.Gone;
            _fabCheckStatus.Visibility = ViewStates.Gone;
        }

        private void ButtonSynchronize_Click(object sender, EventArgs e)
        {
            var unixTime = ConvertToUnixTime(DateTime.Now);
            var buffer = Encoding.UTF8.GetBytes($">set:{unixTime}!");
            _socket.OutputStream.Write(buffer, 0, buffer.Length);

            CheckStatus();
        }

        private void ButtonSimulate_Click(object sender, EventArgs e)
        {
            long unixDateTime = ConvertToUnixTime(DateTime.Now);
            var status = @$"
{{
    ""time"": {unixDateTime}
}}
";
            ProcessStatus(status);
        }

        private static long ConvertToUnixTime(DateTime dateTime)
        {
            var dateTimeOffset = new DateTimeOffset(dateTime);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        private static DateTime ConvertFromUnixTime(long unixTime)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            return dateTimeOffset.LocalDateTime;
        }

        private void ProcessStatus(string rawStatus)
        {
            _textViewRawStatus.Text = rawStatus;

            var status = JsonConvert.DeserializeObject<Status>(rawStatus);
            var clockUnixTime = status.Time;
            _textViewClockUnixTime.Text = clockUnixTime.ToString();

            var clockDateTime = ConvertFromUnixTime(clockUnixTime);
            _textViewClockDate.Text = clockDateTime.ToString("dd/MM/yyyy");
            _textViewClockTime.Text = clockDateTime.ToString("HH:mm:ss");

            var phoneDateTime = DateTime.Now;
            var phoneUnixTime = ConvertToUnixTime(phoneDateTime);
            _textViewPhoneUnixTime.Text = phoneUnixTime.ToString();

            _textViewPhoneDate.Text = phoneDateTime.ToString("dd/MM/yyyy");
            _textViewPhoneTime.Text = phoneDateTime.ToString("HH:mm:ss");

            if (ApproximatelyEqual(phoneUnixTime, clockUnixTime))
            {
                _imageViewUnixTimeDifference.SetBackgroundColor(Color.Green);
                _imageViewTimeDifference.SetBackgroundColor(Color.Green);
            }
            else
            {
                _imageViewUnixTimeDifference.SetBackgroundColor(Color.Red);
                _imageViewTimeDifference.SetBackgroundColor(Color.Red);
            }

            if (_textViewClockDate.Text == _textViewPhoneDate.Text)
            {
                _imageViewDateDifference.SetBackgroundColor(Color.Green);
            }
            else
            {
                _imageViewDateDifference.SetBackgroundColor(Color.Red);
            }
        }

        private bool ApproximatelyEqual(long a, long b)
        {
            if (a > b + 15)
            {
                return false;
            }
            if (a < b - 15)
            {
                return false;
            }
            return true;
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            _socket.Connect();

            _buttonConnect.Visibility = ViewStates.Gone;
            _buttonDisconnect.Visibility = ViewStates.Visible;
            _buttonSynchronize.Visibility = ViewStates.Visible;
            _fabCheckStatus.Visibility = ViewStates.Visible;

            CheckStatus();
        }

        private void CheckStatus()
        {
            var buffer = Encoding.UTF8.GetBytes(">status!");
            _socket.OutputStream.Write(buffer, 0, buffer.Length);

            Thread.Sleep(200);
            var statusBuffer = new byte[1024];
            var attempts = 0;
            var length = 0;
            while (attempts < 10)
            {
                Thread.Sleep(100);
                try
                {
                    length = _socket.InputStream.Read(statusBuffer, length, statusBuffer.Length);
                }
                catch (Exception)
                {
                    break;
                }
                var s = Encoding.UTF8.GetString(statusBuffer, 0, length);
                if (s.TrimEnd().EndsWith("}"))
                {
                    attempts = 10;
                }
                attempts++;
            }

            var status = Encoding.UTF8.GetString(statusBuffer, 0, length).Trim();
            if (status.StartsWith("{") && status.EndsWith("}"))
            {
                ProcessStatus(status);
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
            if (_debugOptionsEnabled)
            {
                _textViewRawStatus.Visibility = ViewStates.Visible;
                _buttonSimulate.Visibility = ViewStates.Visible;
            }
            else
            {
                _textViewRawStatus.Visibility = ViewStates.Gone;
                _buttonSimulate.Visibility = ViewStates.Gone;
            }
        }

        private void FabCheckStatus_OnClick(object sender, EventArgs eventArgs)
        {
            CheckStatus();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
