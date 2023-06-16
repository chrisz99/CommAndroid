
using Android.App;
using Android.Content;

using Android.Graphics;

using Android.Views;
using Android.Widget;
using System.Collections.Generic;


namespace CommAndroid
{


    public class ListViewFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        //Globals
        private Context context;
        public static List<string> items;
        public static List<string> results;


        //Constructor to create list view factory
        public ListViewFactory(Context context)
        {
            this.context = context;
            if (items == null)
                items = new List<string>();


            if (results == null)
                results = new List<string>();

            if(items.Count == 0 && results.Count == 0)
            {
                items.Add("CMD: ");
                results.Add("");
            }

            
        }

        public void OnCreate()
        {
            // Initialize your data source
        
        }

        public void removeLast()
        {
            items.RemoveAt(items.Count - 1);
            results.RemoveAt(results.Count - 1);
        }

        //Add's a command to the listview, removes the prior placeholder CMD text
        public void addCommand(string command,string result)
        {
            if (items == null || items.Count < 0)
            {
                items = new List<string>();
            }
            removeLast();
            items.Add(command);
            results.Add(result);
            items.Add("CMD: ");
            results.Add("");
        }

        //Clear the lists
        public void clearList()
        {
            items.Clear();
            results.Clear();
            items.Add("CMD: ");
            results.Add("");
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

            //Check's if there is items, iterates until there is none
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
                  if(results[position].ToLower().Split(' ')[0] != "invalid")
                        remoteViews.SetTextColor(Resource.Id.results_text, Color.White);
                }
                   
                remoteViews.SetTextViewText(Resource.Id.results_text, results[position]); 
            }
            
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

            Intent fillInIntent = new Intent();
            fillInIntent.PutExtra("command_text", items[position]);
           remoteViews.SetOnClickFillInIntent(Resource.Id.listviewlayout, fillInIntent);




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
        }
    }


}

