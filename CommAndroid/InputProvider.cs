using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;

using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;


namespace CommAndroid
{
    //Label class as activity for Manifest, set theme to custom transparent theme
    [Activity(Label = "TransparentPopupActivity",LaunchMode =LaunchMode.SingleInstance,ScreenOrientation =ScreenOrientation.Portrait, Theme = "@style/Theme.AppCompat.Transparent.NoActionBar")]
    class InputProvider : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Create a view for our input layout, find the EditText view
            View popupView = LayoutInflater.FromContext(this).Inflate(Resource.Layout.widget_input_layout, null);
            EditText cmdText = popupView.FindViewById<EditText>(Resource.Id.cmd_input);

            //Initialize WidgetDataManager, grab theme from SharedPreference data
            WidgetDataManager widgetDataManager = new WidgetDataManager(this);
            int appWidgetId = Intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
            string theme = widgetDataManager.getTheme(appWidgetId);
            string title = widgetDataManager.getTitle(appWidgetId);

            //Set Theme to input popup
            popupView.FindViewById<LinearLayout>(Resource.Id.inputlayout_background).SetBackgroundResource(int.Parse(theme.Split('_')[1]));
            popupView.FindViewById<LinearLayout>(Resource.Id.input_background).SetBackgroundResource(int.Parse(theme.Split('_')[2]));
            popupView.FindViewById<TextView>(Resource.Id.terminalinput_title).SetTextColor(Android.Graphics.Color.ParseColor(theme.Split('_')[3]));
            popupView.FindViewById<TextView>(Resource.Id.terminalinput_title).Text = title + " Input";

            //Event for our EditText field, handles submitting input to the widget provider          
            cmdText.EditorAction += (sender, e) =>
            {
                //Checks if user has completed their input via the complete button
                //Send intent with user input / appWidgetId over to the widget provider
                if(e.ActionId == ImeAction.Done || (e.Event != null && e.Event.Action == KeyEventActions.Down && e.Event.KeyCode == Keycode.Enter))
                {
                    string userInput = cmdText.Text;
                    Intent intent = new Intent(this, typeof(WidgetProvider));
                    intent.SetAction("com.company.CommAndroid.USER_INPUT_SUBMITTED");

                    intent.PutExtra("user_input", userInput);
                    intent.PutExtra("appWidgetId", appWidgetId);
                    intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask); // Add the ClearTop and NewTask flags
                    SendBroadcast(intent);
                    FinishAffinity();
                }
           

               
            };

            //Sets the current view to our input layout
            SetContentView(popupView);


        }
    }


  
    
}
