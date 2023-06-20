using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Net;
using Android.OS;

using Android.Util;

using Android.Widget;
using AndroidX.Preference;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace CommAndroid
{

    [BroadcastReceiver(Label = "CommAndroid", Exported = false, Enabled = true, Name = "com.company.CommAndroid.WidgetProvider",Icon ="@mipmap/terminalappicon")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE"  })]
    [MetaData("android.appwidget.provider", Resource = "@xml/my_widget")]


    //Class to inflate and show the widget
    public class WidgetProvider : AppWidgetProvider
    {
        //Initialize Global Vars for Class
        private Handler handler;
        private WidgetDataManager widgetDataManager;


        //Update method for a widget
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);

            foreach (int appwidgetId in appWidgetIds)
            {
            

                // Build the remote views for the widget
                var widgetView = BuildRemoteViews(context, appwidgetId);

                //LogData from shared preferences
                viewData(context);

                // Update all instances of the widget with the new remote views
                appWidgetManager.NotifyAppWidgetViewDataChanged(appwidgetId, Resource.Id.listView1);
                appWidgetManager.UpdateAppWidget(appwidgetId, widgetView);
       



            }



        }


        //If Widget Is Resized, Re-Build Remote Views
        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);

            //Build New Remote View
            var widgetView = BuildRemoteViews(context, appWidgetId);
    
            // Update widget with the new remote views
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetId, Resource.Id.listView1);
            appWidgetManager.UpdateAppWidget(appWidgetId, widgetView);

        }

        //Method To Log SharedPreference Data In Logs
        public void viewData(Context context)
        {
            ISharedPreferences sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);

            // Get all key-value pairs from SharedPreferences
            IDictionary<string, object> allPreferences = sharedPreferences.All;

            // Display the SharedPreferences data in the terminal
            foreach (KeyValuePair<string, object> preference in allPreferences)
            {
                string key = preference.Key;
                object value = preference.Value;

                // Print the key-value pair to the console
                Log.Debug("Data","Key: " + key + ", Value: " + value);
            }
        }


        //Build the widget view, returns to the update method
        public RemoteViews BuildRemoteViews(Context context, int appwidgetId)
        {
            //Initialize our widget view, basically pointing to our main widget layout xml
            var widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);

            //Initialize WidgetDataManager class to handle Widget Data
            widgetDataManager = new WidgetDataManager(context);
            widgetDataManager.checkInitData(appwidgetId);

            //Initializing an intent for the main widget button click
            var clickIntent = new Intent(context, typeof(WidgetProvider));
            clickIntent.SetAction("com.company.CommAndroid.WIDGET_BUTTON_CLICK");
            clickIntent.PutExtra("appWidgetId", appwidgetId);

            // Initializing a new Intent for the delete button click action
            var deleteIntent = new Intent(context, typeof(WidgetProvider));
            deleteIntent.SetAction("com.company.CommAndroid.DELETE_BUTTON_CLICK");
            deleteIntent.PutExtra("appWidgetId", appwidgetId);

            //Initializing an intent of our WidgetRemoteViewService, to handle the creation of our list view factory
            //Instructing Widget to use our WidgetRemoteViewService with our ListView
            //Setting AppWidgetId as extra intent to query SharedPreferences for Data
            Intent listViewIntent = new Intent(context, typeof(WidgetRemoteViewService));
            listViewIntent.PutExtra("appWidgetId", appwidgetId);
            listViewIntent.SetData(Uri.FromParts("content", appwidgetId.ToString(), null));
            widgetView.SetRemoteAdapter(Resource.Id.listView1, listViewIntent);


            //Set Pending Click Intent to Command Widget Button, Delete Widget Button
            var pendingIntent = PendingIntent.GetBroadcast(context, appwidgetId, clickIntent, PendingIntentFlags.Mutable);
            var pendingDeleteIntent = PendingIntent.GetBroadcast(context, appwidgetId, deleteIntent, PendingIntentFlags.Mutable);

            // Associating the pending intents with the respective buttons in the widget layout
            widgetView.SetOnClickPendingIntent(Resource.Id.delete_button, pendingDeleteIntent);
            widgetView.SetOnClickPendingIntent(Resource.Id.widget_button, pendingIntent);

            //Possible Future Feature

            //Intent itemClickIntent = new Intent(context, typeof(WidgetProvider));
            //itemClickIntent.SetAction("com.company.CommAndroid.LIST_ITEM_CLICK");
            //PendingIntent itemClickPendingIntent = PendingIntent.GetBroadcast(context, 3434514, itemClickIntent, PendingIntentFlags.Mutable);
            //widgetView.SetPendingIntentTemplate(Resource.Id.listView1, itemClickPendingIntent);

            return widgetView;
        }

        //Function that fires when Widget is deleted
        //When Widget is removed from home screen, remove WidgetData from SharedPreferences
        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            widgetDataManager = new WidgetDataManager(context);
            foreach (int appwidgetId in appWidgetIds)
            {
                widgetDataManager.removeLists(appwidgetId);
            }

        }


        //Method to update the listview in widget
        private async void updateListView(Context context, RemoteViews views, bool isDelete, string command, int appWidgetId)
        {
            //Initialize WidgetDataManager Object / AppWidgetManager (update the widget)
            widgetDataManager = new WidgetDataManager(context);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);

            //Set our view to parameter WidgetView
            var widgetView = views;

           

            //Checks bool parameter to see if user wants to clear list
            //If user wants to add a command
            if (isDelete != true)
            {
                //Initialize result that is returned from querying the command through class TerminalCommands
                //Add the command itself, and the results to the list
                string result = await TerminalCommands.queryCommand(command, context, appWidgetManager, appWidgetId, widgetView);
                widgetDataManager.addCommand(command, result, appWidgetId);
               
            }
            //Bool parameter for user clearing list
            else
            {
                widgetDataManager.clearList(appWidgetId);
                widgetView.SetEmptyView(Resource.Id.listView1, Resource.Id.empty_view);

            }

            //Update the widget view
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetId, Resource.Id.listView1);
            appWidgetManager.UpdateAppWidget(appWidgetId, views);


        }



        //Method for receiving broadcasts from broadcast receiver
        public override void OnReceive(Context context, Intent intent)
        {
            //Initialize view for our widget layout
            base.OnReceive(context, intent);


            //Command Click
            if (intent.Action == "com.company.CommAndroid.WIDGET_BUTTON_CLICK")
            {
                //Create a new intent of activity InputProvider
                //Start the input provider activity
                //Pass appWidgetId to user input activity
                int appWidgetId = intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
                Intent inputIntent = new Intent(Application.Context, typeof(InputProvider));
                inputIntent.PutExtra("appWidgetId", appWidgetId);
                inputIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(inputIntent);


            }
            //Clear Click
            else if (intent.Action == "com.company.CommAndroid.DELETE_BUTTON_CLICK")
            {
                //Grab appWidgetId from delete button, build new RemoteViews, clear the list
                int appWidgetId = intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
                var widgetView = BuildRemoteViews(context, appWidgetId);
                updateListView(context, widgetView, true, "",appWidgetId);
            }
            //User input
            else if (intent.Action == "com.company.CommAndroid.USER_INPUT_SUBMITTED")
            {
                //Grab appWidgetId and command from input activity, build new RemoteViews, update the list with command
                string command = intent.GetStringExtra("user_input");
                int appWidgetId = intent.GetIntExtra("appWidgetId",AppWidgetManager.InvalidAppwidgetId);
                var widgetView = BuildRemoteViews(context, appWidgetId);
                updateListView(context, widgetView, false, command,appWidgetId);
            }

            //Possible future feature

            //else if(intent.Action == "com.company.CommAndroid.LIST_ITEM_CLICK")
            //{
            //    Log.Debug("hi", "we got here");
            //    string commandText = intent.GetStringExtra("command_text");
            //}



        }

    }



}