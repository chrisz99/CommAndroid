using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using AndroidX.Core.Widget;

//Declare permissions in android manifest


namespace CommAndroid
{

    //Handles the creation of the List View Factory Class --> Function to return our ListViewFactory
    //Declare as service in android manifest, request permission BIND_REMOTEVIEWS

    [Service(Enabled = true,  Exported = false, Permission = "android.permission.BIND_REMOTEVIEWS")]
    public class WidgetRemoteViewService : RemoteViewsService
    {
        //Returns new ListViewFactory from context and intent
        //Intent holds AppWidgetId data --> used for querying SharedPreferences for list data
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ListViewFactory(this, intent);
        }
    }

}