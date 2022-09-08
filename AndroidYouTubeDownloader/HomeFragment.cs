using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;

namespace AndroidYouTubeDownloader
{
    internal class HomeFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.home_fragment, container, false);
        }
    }
}
