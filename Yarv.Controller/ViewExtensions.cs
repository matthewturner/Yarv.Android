using Android.Views;
using System.Text;

namespace Yarv.Controller
{
    public static class ViewExtensions
    {
        public static void Show(this View view)
        {
            view.Visibility = ViewStates.Visible;
        }

        public static void Hide(this View view)
        {
            view.Visibility = ViewStates.Gone;
        }
    }
}