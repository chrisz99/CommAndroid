using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using Android.Content;
using System.Collections.Generic;
using Android.Widget;
using System.Text;
using AndroidX.Core.SplashScreen;
using Android.Text;
using System.Threading.Tasks;

namespace CommAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/SplashTheme", MainLauncher = true, Icon = "@mipmap/terminalappicon")]
    public class MainActivity : AppCompatActivity
    {
        //Globals
        public static string[] PERMISSIONS = { Manifest.Permission.WriteCalendar, Android.Manifest.Permission.SendSms,
        Android.Manifest.Permission.ReadExternalStorage,Manifest.Permission.ReadContacts,Manifest.Permission.ManageExternalStorage};
        public static bool allPermissionsGranted = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            //Show Splash Screen as shown in new documentation
            AndroidX.Core.SplashScreen.SplashScreen splashScreen = AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);


            //Request Permissions, Set Command Text on Main Activity Page
            RequestPermissionsHelper(this);
            setHelpCommandText(this);


        }

        //Method to set the command text on main page
        private static void setHelpCommandText(Activity activity)
        {
            //Find TextView, Init Commandlist, Create a stringbuilder
            TextView textview = activity.FindViewById<TextView>(Resource.Id.command_list);
            string[] commandList = TerminalCommands.commandList;
            StringBuilder sb = new StringBuilder();

            //Foreach command in the list, add it to stringbuilder with a new line
            foreach (string command in commandList)
            {
                SpannableString commandString = new SpannableString(command);
                sb.Append(commandString);
                sb.Append("\n");
            }

            //Set text to stringbuilder
            textview.SetText(sb.ToString(), TextView.BufferType.Spannable);
        }

        //Method to Request all Permissions Necessary for Terminal App
        //Prompts Users
        private static void RequestPermissionsHelper(Activity activity)
        {


            //Perform Check if user has all file access enabled
            if (!Environment.IsExternalStorageManager)
            {
                //Prompt user about all file access
                Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(activity);
                builder.SetTitle("Permission Required");
                builder.SetMessage("In order for CommAndroid to work as intended; All file access is required.");
                builder.SetPositiveButton("OK", (dialog, which) =>
                {
                    Intent intent = new Intent(Android.Provider.Settings.ActionManageAllFilesAccessPermission);
                    activity.StartActivity(intent);
                });
                builder.SetCancelable(false);
                builder.Show();

            }

            //Create list to hold permissions
            List<string> permissionsList = new List<string>();

            //Iterates through req'd permissions, adding permissions to the list that has not been granted
            foreach (string permission in PERMISSIONS)
            {
                if (ActivityCompat.CheckSelfPermission(activity, permission) != Permission.Granted)
                    permissionsList.Add(permission);
            }

            //If permissions have not been granted
            if (permissionsList.Count > 0)
            {
                //Create a new permissions list for re-asking the user, storing permissions that users have denied
                List<string> reAskPermissions = new List<string>();
                foreach (string permission in permissionsList)
                {
                    if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, permission))
                        reAskPermissions.Add(permission);
                }

                //If users has previously denied permissions, prompt again with an explanation => Re-Request Permissions
                if (reAskPermissions.Count > 0)
                {
                    Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(activity);
                    builder.SetTitle("Permission Required");
                    builder.SetMessage("In order for CommAndroid to work as intended, some permissions are required.");
                    builder.SetPositiveButton("OK", (dialog, which) =>
                    {
                        // Request the permissions after the user acknowledges the explanation
                        ActivityCompat.RequestPermissions(activity, permissionsList.ToArray(), 0);
                    });
                    builder.SetCancelable(false);
                    builder.Show();
                }
                else
                    ActivityCompat.RequestPermissions(activity, permissionsList.ToArray(), 0);




            }


        }

        //Override method to receive permissions results
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == 0)
            {
                foreach (Permission permission in grantResults)
                {
                    if (permission != Permission.Granted)
                    {
                        allPermissionsGranted = false;
                        break;
                    }
                    else
                        allPermissionsGranted = true;
                }
            }
        }
    }
}