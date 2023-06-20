
using Android.App;
using Android.Appwidget;
using Android.Content;

using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;


namespace CommAndroid
{


    public class ListViewFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        //Globals
        private Context context;
        private List<string> items;
        private List<string> results;
        private int appWidgetId;
        private WidgetDataManager widgetDataManager;
        private Handler handler;


        //Constructor to create list view factory
        public ListViewFactory(Context context, Intent intent)
        {
            this.context = context;
            //Checking if items dictionary is null

            this.appWidgetId = int.Parse(intent.Data.SchemeSpecificPart);

            if (items == null)
                items = new List<string>();
            if (results == null)
                results = new List<string>();

            widgetDataManager = new WidgetDataManager(context);
            items = widgetDataManager.getCommands(appWidgetId);
            results = widgetDataManager.getResults(appWidgetId);



      


            
        }

        public void OnCreate()
        {
            // Initialize your data source
        
        }




        public void OnDestroy()
        {
            // Cleanup resources if needed
        }

        public int Count => items.Count;

        //Method to create and set the listview view
        public RemoteViews GetViewAt(int position)
        {
            // Create a RemoteViews object for each item in the data source, Referencing our Layout for our List View
            RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.listview_layout);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);


            if (items != null && items.Count > position)
                {
                    //Sets text in listview to command items and result items
                    //Also checks the commands text to determine what color to output
                    remoteViews.SetTextViewText(Resource.Id.command_text, items[position]);
                    if (results[position].ToLower().Split(' ')[0] == "invalid")
                        remoteViews.SetTextColor(Resource.Id.results_text, Color.Red);
                    else
                        remoteViews.SetTextColor(Resource.Id.results_text, Color.ParseColor("#52db02"));
                    if (items[position].ToLower().Split(' ')[1] == "help" || items[position].ToLower().Split(' ')[1] == "dir" || results[position].ToLower().Split(' ')[0] == "flipping")
                    {
                        if (results[position].ToLower().Split(' ')[0] != "invalid")
                            remoteViews.SetTextColor(Resource.Id.results_text, Color.White);
                    }

                    remoteViews.SetTextViewText(Resource.Id.results_text, results[position]);
                }


            //Create Handler to facilitate Self Scrolling of Listview
            //Set Scroll position, partially update app widget
            //Don't know why this works instead of just setting scroll position, but hey it's android development
            handler = new Handler(Looper.MainLooper);
            handler.PostDelayed(async () =>
            {

                remoteViews.SetScrollPosition(Resource.Id.listView1, widgetDataManager.getCount(appWidgetId));
                appWidgetManager.PartiallyUpdateAppWidget(appWidgetId, remoteViews);

            }, 500);

            //If statement checking whether the position in the list is the last one
            //Sets blinking animation to visible
            if (position == items.Count - 1)
                {
                    remoteViews.SetViewVisibility(Resource.Id.blinking_dot, ViewStates.Visible);
                }
                else
                {
                    remoteViews.SetViewVisibility(Resource.Id.blinking_dot, ViewStates.Gone);
                }



                //Intent fillInIntent = new Intent();
                //fillInIntent.PutExtra("command_text", items[position]);
                //remoteViews.SetOnClickFillInIntent(Resource.Id.listviewlayout, fillInIntent);

         
            return remoteViews;
        } 


        public RemoteViews LoadingView => null;

        public int ViewTypeCount => 1;

        public long GetItemId(int position)
        {
            return position;
        }

        public bool HasStableIds => true;



        public void OnDataSetChanged()
        {
            // Update your data source if needed
            widgetDataManager = new WidgetDataManager(context);
            items = widgetDataManager.getCommands(appWidgetId);
            results = widgetDataManager.getResults(appWidgetId);

        }
    }


}

