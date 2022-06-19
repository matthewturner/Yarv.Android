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

            status.Remove(end + 1, status.Length - end - 1);
            status.Remove(0, start);

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