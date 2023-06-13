using Android.App;
using Android.Appwidget;
using Android.Content;

using Android.OS;

using Android.Util;

using Android.Widget;
using System.IO;
using System.Linq;
using static Android.Icu.Text.Transliterator;
using static Android.Widget.RemoteViews;

namespace CommAndroid
{

    [BroadcastReceiver(Label = "CommAndroid", Exported = false, Enabled = true, Name = "com.company.CommAndroid.WidgetProvider",Icon ="@mipmap/terminalappicon")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE"  })]
    [MetaData("android.appwidget.provider", Resource = "@xml/my_widget")]


    //Class to inflate and show the widget
    public class WidgetProvider : AppWidgetProvider
    {
        //Initialize Global Vars for Class
        public static ListViewFactory listViewFactory;
        private Handler handler;


        //Update method for a widget
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);

            foreach (int appwidgetId in appWidgetIds)
            {
                // Build the remote views for the widget
                var widgetView = BuildRemoteViews(context);




                // Update all instances of the widget with the new remote views
                appWidgetManager.NotifyAppWidgetViewDataChanged(appwidgetId, Resource.Id.listView1);
                appWidgetManager.UpdateAppWidget(appwidgetId, widgetView);
       



            }



        }


        //Build the widget view, returns to the update method
        public RemoteViews BuildRemoteViews(Context context)
        {
            //Initialize our widget view, basically pointing to our main widget layout xml
            var widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);           
           
            //Initializing an intent of our WidgetRemoteViewService, to handle the creation of our list view factory
            //Instructing Widget to use our WidgetRemoteViewService with our ListView
            Intent listViewIntent = new Intent(context, typeof(WidgetRemoteViewService));
            widgetView.SetRemoteAdapter(Resource.Id.listView1, listViewIntent);


            //Initializing an intent for the main widget button click
            var clickIntent = new Intent(context, typeof(WidgetProvider));
            clickIntent.SetAction("com.company.CommAndroid.WIDGET_BUTTON_CLICK");

            // Initializing a new Intent for the delete button click action
            var deleteIntent = new Intent(context, typeof(WidgetProvider));
            deleteIntent.SetAction("com.company.CommAndroid.DELETE_BUTTON_CLICK");


            // Initializing pending intents for the button click actions
            var pendingIntent = PendingIntent.GetBroadcast(context, 5555533, clickIntent, PendingIntentFlags.Mutable);
            var pendingDeleteIntent = PendingIntent.GetBroadcast(context, 55522111, deleteIntent, PendingIntentFlags.Mutable);



            // Associating the pending intents with the respective buttons in the widget layout
            widgetView.SetOnClickPendingIntent(Resource.Id.delete_button, pendingDeleteIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widget_button, pendingIntent);

            Intent itemClickIntent = new Intent(context, typeof(WidgetProvider));
            itemClickIntent.SetAction("com.company.CommAndroid.LIST_ITEM_CLICK");
            PendingIntent itemClickPendingIntent = PendingIntent.GetBroadcast(context, 3434514, itemClickIntent, PendingIntentFlags.Mutable);
            widgetView.SetPendingIntentTemplate(Resource.Id.listView1, itemClickPendingIntent);

  


            // ...

            return widgetView;
        }

        //On Deleted
        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);

        }

        //Method to update the listview in widget
        private async void updateListView(Context context, RemoteViews views, bool isDelete, string command)
        {
            //Create a new list factory and view for our widget
            //Create an AppWidgetManager, reference component name, and get appWidgetIds
            listViewFactory = new ListViewFactory(context);

            var widgetView = views;

            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetProvider)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);

            //Checks bool parameter to see if user wants to clear list
            if (isDelete != true)
            {
                //Initialize string result that is returned from querying the command through class TerminalCommands
                //Add the command itself, and the results to the list
                string result = await TerminalCommands.queryCommand(command, context, appWidgetManager, appWidgetIds);
                listViewFactory.addCommand("CMD: " + command, result);

                //Create Handler to facilitate Self Scrolling of Listview
                //Set Scroll position, partially update app widget
                //Don't know why this works instead of just setting scroll position, but hey it's android development
                handler = new Handler(Looper.MainLooper);
                handler.PostDelayed(async() =>
                {
                       
                    widgetView.SetScrollPosition(Resource.Id.listView1, ListViewFactory.items.Count - 1);
                    appWidgetManager.PartiallyUpdateAppWidget(appWidgetIds, widgetView);

                }, 500);
          


            }
            //Bool parameter for user clearing list
            else
            {
                listViewFactory.clearList();
                widgetView.SetEmptyView(Resource.Id.listView1, Resource.Id.empty_view);

            }

            //Update the widget view
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetIds, Resource.Id.listView1);
            appWidgetManager.UpdateAppWidget(componentName, views);


        }



        //Method for receiving broadcasts from broadcast receiver
        public override void OnReceive(Context context, Intent intent)
        {
            //Initialize view for our widget layout
            base.OnReceive(context, intent);

            //var widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);

            //Rebuild the view, re-setting the intents ---> Goal is to fix bug where widget stops working after some time
            //Either this or settings flags to mutable
            var widgetView = BuildRemoteViews(context);

            //Command Click
            if (intent.Action == "com.company.CommAndroid.WIDGET_BUTTON_CLICK")
            {
                //Create a new intent of activity InputProvider
                //Start the input provider activity
                Intent inputIntent = new Intent(Application.Context, typeof(InputProvider));
                inputIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(inputIntent);


            }
            //Clear Click
            else if (intent.Action == "com.company.CommAndroid.DELETE_BUTTON_CLICK")
            {
                updateListView(context, widgetView, true, "");
            }
            //User input
            else if (intent.Action == "com.company.CommAndroid.USER_INPUT_SUBMITTED")
            {
                string command = intent.GetStringExtra("user_input");
                updateListView(context, widgetView, false, command);
            }
            else if(intent.Action == "com.company.CommAndroid.LIST_ITEM_CLICK")
            {
                Log.Debug("hi", "we got here");
                string commandText = intent.GetStringExtra("command_text");





            }



        }

    }



}