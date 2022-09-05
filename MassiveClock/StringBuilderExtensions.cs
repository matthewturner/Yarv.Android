using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassiveClock
{
    public static class StringBuilderExtensions
    {
        private class Nest
        {
            public int Start { get; set; }
            public int End { get; set; }
        }

        /// <summary>
        /// Filters out the last json payload within the string
        /// and returns true if found
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool FilterJson(this StringBuilder status)
        {
            var nest = status.LastNest();

            if (nest == null)
            {
                return false;
            }

            status.Remove(nest.End + 1, status.Length - nest.End - 1);
            status.Remove(0, nest.Start);

            return true;
        }

        private static Nest LastNest(this StringBuilder builder)
        {
            var stack = new Stack<Nest>();

            for (var i = builder.Length - 1; i >= 0; i--)
            {
                if (builder[i] == '}')
                {
                    stack.Push(new Nest { End = i });
                    continue;
                }
                if (builder[i] == '{')
                {
                    if (stack.TryPop(out Nest result))
                    {
                        result.Start = i;
                        if (!stack.Any())
                        {
                            return result;
                        }
                    }
                    else
                    {
                        // dodgy input, carry on searching
                    }
                }
            }

            return null;
        }
    }
}