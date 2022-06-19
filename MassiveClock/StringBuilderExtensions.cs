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

namespace MassiveClock
{
    public static class StringBuilderExtensions
    {
        public static bool ExtractJson(this StringBuilder status)
        {
            var start = status.LastIndexOf('{');
            var end = status.LastIndexOf('}');

            if (start == -1 || end == -1)
            {
                return false;
            }

            if (start > end)
            {
                return false;
            }

            status.Remove(0, start);
            status.Remove(end, status.Length - end);

            return true;
        }

        public static int LastIndexOf(this StringBuilder builder, char ch)
        {
            for (var i = builder.Length - 1; i >= 0; i--)
            {
                if (builder[i] == ch)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}