using Android.App;
using Android.Content;
using Android.Icu.Util;
using Android.Provider;
using Android.OS;
using Android.Telephony;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using Java.Util;
using System;
using System.IO;
using Xamarin.Essentials;

using System.Threading.Tasks;

using Contacts = Xamarin.Essentials.Contacts;
using Android.Content.PM;

using Android.Util;
using Android.Appwidget;
using Android.Media;
using Android.App.Usage;


namespace TerminalApp
{
    public class TerminalCommands
    {
        //Helper String for printing the command list
        //Add Commands here to display in Terminal
        public static string[] commandList = { "txt 'name/num' 'message'", "rem 'hours:minutes' 'title'", "dir 'path'", "datclear 'appname'","coinflip" };


        //Text Message Method
        private static void sendText(string textMessageText, string phoneNumber)
        {
            SmsManager smsManager = SmsManager.Default;

            // Split the message into parts if it exceeds the maximum length
            IList<string> parts = smsManager.DivideMessage(textMessageText);

            // Send each part of the message separately
            foreach (string part in parts)
            {
                smsManager.SendTextMessage(phoneNumber, null, part, null, null);
            }
        }

        //Text Message Helper Function
        private static string sendTextHelper(string[] arguments, Context context)
        {
            //Checking if all the neccessary arguments are added
            if (arguments.Length >= 2)
            {
                //Initializing and setting phone number, and textMessage
                //Grabs contact / phone number between indexes of 'contact/phone'
                //Checks if it is a contact or all digits
                string phoneNumber = "";
                string argumentsText = string.Join(" ", arguments.Skip(0));
                int startIndex = argumentsText.IndexOf('\'');
                int endIndex = argumentsText.LastIndexOf('\'');
                if (startIndex > 0 || endIndex > 0)
                    phoneNumber = argumentsText.Substring((startIndex + 1), (endIndex - startIndex) - 1);
                else
                    return "Invalid Syntax (txt 'name/num' 'message')";
                bool isNumber = phoneNumber.All(c => char.IsDigit(c) || c == '-');
                string textMessage = string.Join("", argumentsText.Skip(endIndex + 1)).TrimStart();

                //If it is all digits, send text right away, if it is a contact, search contacts and grab number
                if (isNumber)
                {
                    sendText(textMessage, phoneNumber);
                }
                else
                {
                    Task<string> grabNumberTask = getPhoneNumber(phoneNumber, context);
                    string result = grabNumberTask.GetAwaiter().GetResult();
                    if (result == "none")
                    {
                        return "Invalid Contact (txt 'name/num' 'message')";
                    }
                    else
                        sendText(textMessage, result);
                }

                
                return "----> Sent Message";

            }
            else
                return "Invalid Syntax (txt 'name/num' 'message')";
        }

        //Checks if queryCommand or other helper commands return an error
        public static bool isError(string result)
        {
            string errorMsg = result.Split(' ')[0];

            if (errorMsg.ToLower() == "invalid")
                return true;
            else
                return false;
        }

        //Prints a command list for user
        public static string printHelp()
        {
            string helpString = "List of Commands: \n";
            foreach (string commands in commandList)
            {
                helpString +=  "-->" + commands + "\n";
            }
            return helpString;
        }

        //Actual function to set a reminder
        private static void setReminder(string reminderMessage, int hour, int minute, Context context)
        {
           
            //New ContentValue object to hold reminder event information
            ContentValues eventValues = new ContentValues();
            eventValues.Put(CalendarContract.Events.InterfaceConsts.CalendarId, 1);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Title, reminderMessage);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Description, reminderMessage);

            //Create a datetime object to grab current time
            DateTime currentDate = DateTime.Now;
            DateTimeOffset currentDateTimeOffset = DateTimeOffset.Now;

            // Set the start time
            DateTimeOffset startDateTime = new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day, hour, minute, 0, currentDateTimeOffset.Offset);
            long startMillis = startDateTime.ToUnixTimeMilliseconds();
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtstart, startMillis); // Replace with the desired date and time

            // Set the end time (1 hour after the start time)
            DateTimeOffset endDateTime = startDateTime.AddHours(1);
            long endMillis = endDateTime.ToUnixTimeMilliseconds();
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend, endMillis);

            //Create TimeZoneInfo object to grab current time zone
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            string timeZoneId = localTimeZone.Id;

            // Convert the local timezone to the desired format
            string eventTimezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId).StandardName;

            //Add time zone information to eventValues
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, eventTimezone); // Replace with the desired timezone
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventEndTimezone, eventTimezone); // Replace with the desired timezone
            eventValues.Put(CalendarContract.Events.InterfaceConsts.HasAlarm, 1);

            //Insert into calender
            Android.Net.Uri eventUri = context.ContentResolver.Insert(CalendarContract.Events.ContentUri, eventValues);
            long eventID = long.Parse(eventUri.LastPathSegment);


            //Initialize array to set reminder times
            //Foreach loop iterating through reminder times, creating a new ContentValues object to hold reminder alert information
            //Add each alarm
            int[] reminderTimes = { 5, 15, 30, 60 };

            foreach (int time in reminderTimes)
            {
                ContentValues reminderValues = new ContentValues();
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.EventId, eventID);
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Alert);
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, time); // Replace with the desired reminder time in minutes

                Android.Net.Uri reminderUri = context.ContentResolver.Insert(CalendarContract.Reminders.ContentUri, reminderValues);
                long reminderID = long.Parse(reminderUri.LastPathSegment);
            }

         

        }

        //Helper function to set reminder
        private static string setReminderHelper(string[] arguments, Context context)
        {

            if(arguments.Length >= 2)
            {
                //Initialize variables for time, reminder message
                string time = arguments[0];
                string reminderMessage = string.Join(" ", arguments.Skip(1));
                string[] timeParts = time.Split(':');

                if (timeParts.Length != 2 || !int.TryParse(timeParts[0], out int hour) || !int.TryParse(timeParts[1], out int minutes))
                {
                    return "Invalid Syntax (rem 'hours:minutes' 'message')";
                }


                //Checks logic and syntax
                if (hour > 12 || minutes > 60 || hour < 0 || minutes < 0)
                    return "Invalid Syntax (rem 'hours:minutes' 'message')";

                setReminder(reminderMessage, hour, minutes, context);
                return "----> Reminder Set for " + hour + ":" + minutes;
            }
            else
                return "Invalid Syntax (rem 'hours:minutes' 'message')";

          

        }

        //Method to print directories and files
        private static string viewDirectory(string directory)
        {
            //Base Case if no directory is specified
            if (directory == "")
                directory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;


            //Initialize strings
            string output = "";
            string[] directoryList = Directory.GetDirectories(directory);
            string[] fileList = Directory.GetFiles(directory);

            //Check if directory or files are empty
            if (fileList.Length > 0 || directoryList.Length > 0)
            {
                output += "Directory:\n";
                //Using += operators, add directories and file names to output string
                foreach (string direct in directoryList)
                {
                    output += "-->" + Path.GetFileName(direct) + "\n";
                }

                foreach (string file in fileList)
                {
                    output += "-->" + Path.GetFileName(file) + "\n";
                }

            }
            else
                output = "----> Empty";

  

            //Return output
            return output;

        }

        //Helper function to view directories
        private static string viewDirectoryHelper(string[] arguments)
        {
            //Method with no arguments default to default directory
            string defaultDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            //Checks Arguments Length
            if (arguments.Length >= 1)
            {
                //Initialize directory string to default directory + arguments
                //Check if Path exists, if so viewDirectory
                string directoryPath = arguments[0];
                directoryPath = Path.Combine(defaultDirectory,directoryPath);
                if (Directory.Exists(directoryPath))
                    return viewDirectory(directoryPath);
                else
                    return "Invalid Path (dir 'path')";

            }
            else
                return viewDirectory("");
        }

        //Helper Function to find user phone numbers
        private static async Task<string> getPhoneNumber(string contactName, Context context)
        {
            //Initialize var contacts, to hold contacts => to list
            var contacts = await Contacts.GetAllAsync();
            List<Contact> contactList = contacts.ToList();

            //Creates a contact object, returns first element that matches display name
            //Lambda expression (C as var for contact object, as c.DisplayName == contactName)
            Contact contact = contactList.FirstOrDefault(c => c.DisplayName.ToLower() == contactName.ToLower());

            //Checking if nothing was found
            if (contact != null)
            {
                //If found check phone numbers, grab first => phoneNumber
                var phoneNumber = contact.Phones.FirstOrDefault()?.PhoneNumber;
                if (!string.IsNullOrEmpty(phoneNumber))
                    return phoneNumber;
            }

            return "none";
          

        }

        //Function to open settings app to clear app data
        private static string clearAppData(string appName)
        {
            //Check if appname given is empty or null
            if (string.IsNullOrEmpty(appName))
                return "Invalid Syntax (datclear 'appname')";

            try
            {
                //Try and find package name by label
                //Launch settings page of package
                string packageName = getPackageName(appName);
                if (packageName != null)
                {
                    Intent intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                    intent.SetData(Android.Net.Uri.FromParts("package", packageName, null));

                    // Start the activity with the intent
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);

                    return "Data settings for " + appName;
                }
                else
                    return "Invalid Package Name";

          
            }
            catch (System.Exception ex)
            {
                return "Invalid Syntax (datclear 'appname')";
            }
     
        }

        //Helper Function to open settings app
        private static string clearAppDataHelper(string[] arguments)
        {
            //Set appname
            string appName = arguments[0];
            if (string.IsNullOrEmpty(appName))
                return "Invalid Syntax (datclear 'appname')";

            //Return results of clearAppData
            return clearAppData(appName);
        }

        //Helper function to get package name
        private static string getPackageName(string appName)
        {
            //Initialize PackageManager to grab list of installed apps
            PackageManager pm = Application.Context.PackageManager;
            IList<ApplicationInfo> installApps = pm.GetInstalledApplications(PackageInfoFlags.MatchAll);

            //Iterate through each app, return packageName if match is found
            foreach(ApplicationInfo appInfo in installApps)
            {
                string packageName = appInfo.PackageName;
                string name = pm.GetApplicationLabel(appInfo).ToString();
                Log.Debug("Appname:", name + " Package Name: " + packageName);

                if (name.ToLower() == appName.ToLower())
                    return packageName;
            }

            //Return null if not
            return null;
        }

        //Coin Flip Method, Async Because Pseudo Flipping Animation
        private async static Task<string> coinFlip(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            //Create new random object r
            //Change ListView items to coinflip, and flipping for user view => Update Widget
            System.Random r = new System.Random();
            ListViewFactory.items[ListViewFactory.items.Count - 1] = "CMD: coinflip ";
            ListViewFactory.results[ListViewFactory.results.Count - 1] = "flipping";
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetIds, Resource.Id.listView1);

            //Play Flipping Sound, Async method to wait for coin flip
            MediaPlayer mediaPlayer = MediaPlayer.Create(context, Resource.Raw.coinflipsound);
            mediaPlayer.Start();

            await Task.Delay(900);
            
            //If random returns 0, return heads, else tails
            if (r.Next(2) == 0)
                return "--> Heads";
            else
                return "--> Tails";
        }

        private static string displayStorageInfo(Context context)
        {
            long totalSize = 0;
            StorageStatsManager storageStatsManager = (StorageStatsManager)context.GetSystemService(Context.StorageStatsService);
            PackageManager pm = context.PackageManager;
            IList<ApplicationInfo> installedApps = pm.GetInstalledApplications(PackageInfoFlags.MatchAll);

            foreach (ApplicationInfo appInfo in installedApps)
            {
             
             


                // Get the package information for the application
                PackageInfo packageInfo = pm.GetPackageInfo(appInfo.PackageName, 0);
                string packageName = packageInfo.PackageName;

                // Get the path to the APK file
                string apkPath = packageInfo.ApplicationInfo.SourceDir;
                Java.Util.UUID dataPath = appInfo.StorageUuid;


 

                long appSize = GetFileSize(apkPath);

                // Get the size of the APK file
                long apkSize = new Java.IO.File(apkPath).Length();

                totalSize += apkSize;
            }

            return "--> " + FormatFileSize(totalSize);



        }
        private static long GetFileSize(string filePath)
        {
            long size = 0;

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Exists)
                {
                    size = fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the process
                Console.WriteLine("Error calculating file size: " + ex.Message);
            }

            return size;
        }

        private static string FormatFileSize(long size)
        {
            if (size <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int digitGroups = (int)(Math.Log10(size) / Math.Log10(1024));
            double adjustedSize = size / Math.Pow(1024, digitGroups);

            return $"{adjustedSize:0.##} {units[digitGroups]}";
        }

        private static long GetDirectorySize(string directoryPath)
        {
            long size = 0;

            try
            {
                Java.IO.File directory = new Java.IO.File(directoryPath);

                if (directory.Exists())
                {
                    Java.IO.File[] files = directory.ListFiles();

                    foreach (Java.IO.File file in files)
                    {
                        if (file.IsFile)
                            size += file.Length();
                        else if (file.IsDirectory)
                            size += GetDirectorySize(file.AbsolutePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calculating directory size: " + ex.Message);
            }

            return size;
        }


        //Main function that takes a command, (string), and breaks it up to see what method to fire
        public static async Task<string> queryCommand(string commandText, Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds )
        {

            //If Statement for no command text entered
            if (commandText == "")
                return "Invalid Command. Try using 'help'";


            //Splitting up the received command by white spaces, initializing string targetCommand, and string[] args
            //Setting targetCommand to the first element (Target Command), and arguments to everything pass that
            string[] commandTextSplit = commandText.Split(new char[] { ' ' });
            string targetCommand = commandTextSplit[0];
            string[] arguments = commandTextSplit.Skip(1).ToArray();

            //Check if the command entered has any elements in it   
            if (commandTextSplit.Length > 0 )
            {
                //Initialize string result, to see what happened with the command
                //Switch Statements based on targetCommand, if no command is entered => 'Invalid Command'
                string result;

                switch (targetCommand.ToLower())
                {                   
                    //Text Message Case
                    case "txt":
                        {
                           return sendTextHelper(arguments, context);
                        }
                    //Reminder Case
                    case "rem":
                        {
                            return setReminderHelper(arguments, context);
                        }
                    case "dir":
                        {
                            return viewDirectoryHelper(arguments);
                        }
                    case "datclear":
                        {
                            return clearAppDataHelper(arguments);
                        }
                    case "coinflip":
                        {
                            return await coinFlip(context,appWidgetManager,appWidgetIds);
                        }
                    case "stordat":
                        {
                            return displayStorageInfo(context);
                        }

                    //Command List
                    case"help":
                        return printHelp();

                    //No Command Found
                    default: return "Invalid Command";
                }
            }
            else
            {
                return "Invalid Command";
            }

            return "true";
        }

        
    }
}