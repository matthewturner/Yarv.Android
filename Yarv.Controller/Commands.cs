using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarv.Controller
{
    public static class Commands
    {
        public const string Stop = "stp";
        public const string Forward = "fwd";
        public const string Reverse = "rev";
        public const string Left = "lft";
        public const string Right = "rht";
        public const string BearLeftForward = "blf";
        public const string BearRightFoward = "brf";
        public const string BearLeftReverse = "blr";
        public const string BearRightReverse = "brr";
        public const string SetSpeed = "spd";
        public const string EnableAutoPilot = "aon";
        public const string DisableAutoPilot = "aof";
    }
}