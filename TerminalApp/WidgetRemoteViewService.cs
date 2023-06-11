using Android.App;
using Android.Content;
using Android.Widget;
using AndroidX.Core.Widget;

//Declare permissions in android manifest


namespace TerminalApp
{

    //Handles the creation of the List View Factory Class --> Function to return our ListViewFactory
    //Declare as service in android manifest, name is pointer for location

    [Service(Enabled = true,  Exported = false, Permission = "android.permission.BIND_REMOTEVIEWS")]
    public class WidgetRemoteViewService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ListViewFactory(this);
        }
    }

}