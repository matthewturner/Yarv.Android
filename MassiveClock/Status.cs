using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassiveClock
{
    public class Status
    {
        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("schedules")]
        public Dictionary<string, List<bool>> Schedules { get; set; }
    }
}