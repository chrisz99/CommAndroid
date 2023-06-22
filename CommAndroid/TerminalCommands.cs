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
using Android.OS.Storage;
using System.Drawing;
using System.ComponentModel.Design;
using AndroidX.Activity;
using Java.Util.Jar;
using System.Text.RegularExpressions;
using Android.Graphics.Drawables;
using Android.Content.Res;
using Android.Views;

namespace CommAndroid
{
    public class TerminalCommands
    {
        //Global Vars
        //Helper String for printing the command list
        //Add Commands here to display in Terminal
        public static string[] commandList = { "txt 'name/num' 'message'", "rem 'hours:minutes' 'title'", "dir 'path'","del 'filepaths'","mkdir 'paths' 'name'", "rmdir 'paths' 'name'","copy 'sourcepath' 'destinationpath'",
            "ren 'sourcepath' 'newname'","datclear 'appname'","stordat","coinflip","title 'name'", "theme 'color'", "wipewidgetdata","help", "help colors"};

        public static string[] colorList = { "default", "black", "orange", "red", "pink", "purple", "green" };

        private static WidgetDataManager widgetDataManager;


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
                helpString +=  "--> " + commands + "\n";
            }
            return helpString;
        }

        public static string printColors()
        {
            string colorString = "List of Colors: \n";
            foreach(string color in colorList)
            {
                colorString += "--> " + color + "\n";
            }
            return colorString;
        }

        public static string printHelpHelper(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0].ToLower().Trim() == "colors")
                    return printColors();
                else
                    return "Invalid Command. Did you mean (help colors)?";
            }
            else
                return printHelp();
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

            if(arguments.Length == 2)
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
        private static async Task<string> coinFlip(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
        {
            //Create new random object r
            //Change ListView items to coinflip, and flipping for user's view => Update Widget
            System.Random r = new System.Random();
            widgetDataManager = new WidgetDataManager(context);
            widgetDataManager.checkInitData(appWidgetId);
            List<string> commands = widgetDataManager.getCommands(appWidgetId);
            List<string> results = widgetDataManager.getResults(appWidgetId);
            commands[commands.Count - 1] = "CMD: coinflip";
            results[results.Count - 1] =  "flipping";
            widgetDataManager.saveLists(appWidgetId, commands, results);
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetId, Resource.Id.listView1);

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

        //Function to display available storage on device
        //Added approximation sign because because past Android 12 grabbing this type of information became difficult
        //Off by a bit
        private static string displayStorageInfo(Context context)
        {

            Java.IO.File file = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory.Path);
            var freeSpace = FormatFileSize(file.FreeSpace);


            return "--> ~" + freeSpace + " free space available";

        }

        //Formats file size
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

        //Function to create a folder given a path and folderName
        private static string createFolder(string path, string folderName)
        {
            //Create Java.IO.File with parameter for path
            Java.IO.File folder = new Java.IO.File(path);

            //If folder doesn't exist => Create Folder
            if (!folder.Exists())
                folder.Mkdirs();
            
            return folderName + " successfully created";
        }

    
  
        //Helper function to create a folder given a string[] args
        private static string createFolderHelper(string[] arguments)
        {
            //Check if arguments are greater than 2, provided path and foldername
            if (arguments.Length >= 2)
            {
                //Initialize path and foldername based on args
                string path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, arguments[0]);
                string folderName = arguments[1];

                //Checks if directory exists with path
                //Initialize string result, foldernamelist (formatting string), folderNames[],
                //and a list to hold folders that already exist
                if (Directory.Exists(path))
                {
                    
                        string result = "";
                    string folderNameList = "";
                        string[] folderNames = arguments.Skip(1).ToArray();
                        List<string> alreadyExists = new List<string>();

                        //Checks if folder already exists, adds it to the list, add foldername to formatting string
                        foreach(string folder in folderNames)
                        {
                            string combinedPathWithAddedFolder = Path.Combine(path, folder); 
                            if (Directory.Exists(combinedPathWithAddedFolder))
                            {
                                alreadyExists.Add(combinedPathWithAddedFolder);
                                result += folder + ", ";
                            }
                        folderNameList += folder + ", ";
                        }

                        //Check if any exists, if so, return invalid operation
                        if (alreadyExists.Count > 0)
                            return "Invalid Operation. Folder " + folderNameList + " already exists.";
                        //If none exists with provided foldernames => Create folders
                        else
                        {
                            foreach(string folder in folderNames)
                            {
                                string combinedPathWithAddedFolder = Path.Combine(path,folder);
                                createFolder(combinedPathWithAddedFolder, folder);
                            }
                            return folderNameList + " were successfully created.";
                        }

                    
                }
                //If arguments is less than 2
                //Initialize same vars
                else
                {
                    string result = "";
                    string folderNameList = "";
                    List<string> alreadyExists = new List<string>();

                    //Iterate through each folder, if directory exists add it to the list
                    //Add folder name to formatting strings
                    foreach(string folder in arguments)
                    {
                        string combinedPathWithAddedFolder = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                        if (Directory.Exists(combinedPathWithAddedFolder))
                        {
                            alreadyExists.Add(folder);
                            result += folder + ", ";
                        }                       
                        folderNameList += folder + ", ";
                    }

                    //Checks if any exists already, if so, return invalid operation
                    if (alreadyExists.Count > 0)
                        return "Invalid Operation. Folder " + folderNameList + " already exists.";
                    //If they don't exist => Create Folders
                    else
                    {
                        foreach(string folder in arguments)
                        {
                            string combinedPathWithAddedFolder = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                            createFolder(combinedPathWithAddedFolder, folder);
                        }
                        return folderNameList + " were successfully created.";
                    }

                }
            }
            //If args length is one
            //Create folder at default path
            else if (arguments.Length == 1)
            {
                string path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, arguments[0]);
                if (!Directory.Exists(path))
                    return createFolder(path, arguments[0]);
                else
                    return "Invalid operation. " + arguments[0] + " already exists.";
            }
            else
                return "Invalid Syntax (mkdir 'path' 'name')";

      
        }

        //Function to delete a folder given a path, and a folderName
        private static string deleteFolder(string path, string folderName)
        {
            //Create new Java.IO.File with parameter path
            Java.IO.File folder = new Java.IO.File(path);

            //Checks if folder exists, if so, => Delete folder
            if (folder.Exists())
            {
                Directory.Delete(path, true);
                return folderName + " successfully deleted";
            }
            //If not return invalid operation    
            else
                return "Invalid Operation. " + folderName + " doesn't exist.";
        }

        //Delete folder helper function given a string[] args
        private static string deleteFolderHelper(string[] arguments)
        {
            //Checks if args length is greater than 2
            //Initialize defaultPath with args for path
            if (arguments.Length >= 2)
            {
                string defaultPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, arguments[0]);

                //If directory exists with path given
                //Initialize string[] for folder names, strings for result and list, int for existCount, and list for doesn't Exist
                //Since we are not making use of a CD command, we have to utilize other means of figuring out whether the user wants.
                //to delete at the default directory, or with the path specified. We do this by checking the strings in the arguments.
                //Checking if they are foldernames, and if they exist as a directory and incrementing the existCount int
                //If the existCount is greater than 0, we know the user wants to delete at the default directory.

                if (Directory.Exists(defaultPath) || Directory.Exists(removeUnderscore(defaultPath)))
                {
                    string[] folderNames = arguments.Skip(1).ToArray();
                    for(int i = 0; i < folderNames.Length; i++)
                        folderNames[i] = removeUnderscore(folderNames[i]);
                    string result = "";
                    int existCount = 0;
                    string folderNameList = "";
                    List<string> doesntExist = new List<string>();

                    //Check if folder doesn't exist at path, and checking if it does exist as a folder in the default directory
                    foreach (string folder in folderNames)
                    {
                        string checkPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath,folder);
                        string combinedPath = Path.Combine(defaultPath, folder);
                        if(!Directory.Exists(combinedPath))
                        {
                            doesntExist.Add(folder);
                            result += folder + ", ";
                        }
                        if(Directory.Exists(checkPath))
                        {
                            existCount++;
                        }

                        folderNameList += folder + ", ";
                    }

                    //If any exists in the default directory
                    //Delete at the default directory
                    if(existCount > 0)
                    {
                        //Create new formatting string and List for deleting at default directory                       

                        for (int i = 0; i < arguments.Length; i++)
                        {
                            arguments[i] = removeUnderscore(arguments[i]);  
                        }

                        List<string> doesntExist2 = new List<string>();
                        string folderNameList2 = "";
                        result = "";

                        //Check if each folder in arguments doesn't exist, adding it to the list
                        foreach (string folder in arguments)
                        {                     
                            string combinedPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                            if (!Directory.Exists(combinedPath))
                            {
                                doesntExist2.Add(folder);
                                result += folder + ", ";
                            }
                            folderNameList2 += folder + ", ";
                        }

                        //If it doesn't exist => Invalid Operation
                        if (doesntExist2.Count > 0)
                            return "Invalid Operation. Folder " + result + " doesn't exist.";
                        //Otherwise delete the folders in the default directory
                        else
                        {
                            foreach (string folder in arguments)
                            {
                                string combinedPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                                deleteFolder(combinedPath, folder);
                            }
                            return folderNameList2 + " were successfully deleted.";
                        }
                    }
                    //If Exist Count is 0 and Doesnt exist count is 0 than delete at path specified by user
                    if (doesntExist.Count == 0)
                    {
                        foreach (string folder in folderNames)
                        {
                            string combinedPath = Path.Combine(defaultPath, folder);
                            deleteFolder(combinedPath, folder);
                        }
                        return folderNameList + " were successfully deleted.";
                    }
                    else
                        return "Invalid Operation. Folder " + result + " doesn't exist.";

             
                }
                //If directory doesn't exist at path (Default Directory)
                else
                {
                    //Initialize vars
                    List<string> doesntExist = new List<string>();
                    string result = "";
                    string folderNameList = "";
                    foreach(string folder in arguments)
                    {
                        string combinedPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                        if (!Directory.Exists(combinedPath))
                        {
                            doesntExist.Add(folder);
                            result += folder + ", ";
                        }
                        folderNameList += folder + ", ";
                    }

                    //Check if any doesn't exist => Invalid Operation
                    if (doesntExist.Count > 0)
                        return "Invalid Operation. Folder " + result + " doesn't exist.";
                    //Otherwise delete folders at default directory
                    else
                    {
                        foreach(string folder in arguments)
                        {
                            string combinedPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, folder);
                            deleteFolder(combinedPath, folder);
                        }
                        return folderNameList + " were successfully deleted.";
                    }
                }
              
    
            }
            //Base Case with Default Directory
            else if (arguments.Length == 1)
            {
                string path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, arguments[0]);
                return deleteFolder(path, arguments[0]);
            }
            //Bad syntax
            else
                return "Invalid Syntax rmdir 'path' 'name'";
        }



        //Helper function to copy a directory and files
        private static void copyDirectoryHelper(string sourceDirPath, string destinationDirPath)
        {
            Directory.CreateDirectory(destinationDirPath);

            foreach(string dirPath in Directory.GetDirectories(sourceDirPath))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDirPath, destinationDirPath));
            }

            foreach (string newPath in Directory.GetFiles(sourceDirPath))
            {
                File.Copy(newPath, newPath.Replace(sourceDirPath, destinationDirPath));
            }


        }

        //Helper function to determine if a path is a file or directory
        private static string isFile(string path)
        {
            if (File.Exists(path))
                return "file";
            else if (Directory.Exists(path))
            {
                FileAttributes fa = File.GetAttributes(path);
                if (fa.HasFlag(FileAttributes.Directory))
                    return "directory";
                else
                    return "notexist";
            }
            else return "notexist";
      



        }

        //Helper function to remove underscores from paths
        private static string removeUnderscore(string path)
        {
            int nameIndex = path.LastIndexOf("/");
            string name = path.Substring(nameIndex + 1);
            string newName = name.Replace('_', ' ');
            string newPath = path.Replace(name, newName);

            return newPath;
        }

        //Helper function to handle duplicate file count, adds or changes number to the end of name of file. (test.txt) => (test_1.txt)
        private static string changeFileCount(string name, int count)
        {
            int extensionIndex = name.LastIndexOf(".");
            string extension = name.Substring(extensionIndex);
            string fileName = name.Substring(0, extensionIndex);
            if (Regex.IsMatch(fileName,@"_\d"))
            {
                fileName = fileName.Substring(0,fileName.LastIndexOf('_'));
                fileName = removeUnderscore(fileName);  
                return removeUnderscore(fileName) + "_" + count + extension;
            }
            else
                return removeUnderscore(fileName) + "_" + count + extension;
        }

        //Copy Helper Command
        //Copies Folders and Files, Handles Cases Where Folders or Files has Spaces with '_' char
        //copy 'sourcePath' 'destinationPath'
        private static string copyHelper(string[] arguments)
        {
            //Globals
            string defaultPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            //Args length 2 (copy 'sourcepath' 'destination')
            if (arguments.Length == 2)
            {
                //Initialize a string sourcePath by first argument
                //Extract name from sourcepath substring
                //Create initial destination path
                string sourcePath = Path.Combine(defaultPath, arguments[0]);
                int nameIndex = sourcePath.LastIndexOf('/');
                string name = sourcePath.Substring(nameIndex);
                string destinationPath = Path.Combine(defaultPath, arguments[1]);
                int copyCount = 0;

                //Checks if sourcepath is a directory
                if (isFile(sourcePath) == "directory")
                {
                    //Checks if destination exists
                    if (Directory.Exists(destinationPath))
                    {
                       
                        //Adds folder name to end of destination path
                        destinationPath += name;
                        
                        //Iterate until we get a directory that doesn't exist
                        while (Directory.Exists(destinationPath))
                        {
                            //Increment copy counter
                            //Remove Previous Name, add new one with copy counter
                            copyCount++;
                            destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));
                            destinationPath += name + "_" + copyCount;
                        }
                        //Do the copying
                        copyDirectoryHelper(sourcePath, destinationPath);
                        return "Directory " + destinationPath.Substring(destinationPath.LastIndexOf('/')) + " successfully copied to destination " + Path.Combine(defaultPath, arguments[1]);
                    }
                    //If destination directory has underscore
                    else if (Directory.Exists(removeUnderscore(destinationPath)))
                    {
                        //Remove underscore, add name
                        destinationPath = removeUnderscore(destinationPath);
                        destinationPath += removeUnderscore(name);

                        //Iterate until directory doesn't exist
                        while(Directory.Exists(destinationPath))
                        {
                            //Increment copy counter
                            //Substring add copy counter
                            copyCount++;
                            destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));
                            destinationPath += removeUnderscore(name) + "_" + copyCount;
                        }
                        //Do the copying
                        copyDirectoryHelper(sourcePath, destinationPath);
                        return "Directory " + destinationPath.Substring(destinationPath.LastIndexOf('/')) + " successfully copied to destination " + Path.Combine(defaultPath, arguments[1]);
                    }
                    else
                        return "Invalid Operation. Directory doesn't exist at destination " + destinationPath; 

                }
                //If source is a file
                else if (isFile(sourcePath) == "file")
                {
                    //Checks if destination exists
                    if (Directory.Exists(destinationPath))
                    {
                        
                       //Iterate until file doesn't exist
                        while (File.Exists(destinationPath + name))
                        {
                            //Add copy counter
                            copyCount++;
                            name = changeFileCount(name, copyCount);
                            
                        }
                        //Do the copying
                        File.Copy(sourcePath, destinationPath + name, true);
                        return "File " + name + " has been successfully copied to destination " + destinationPath;
                    }
                    //If destination has a underscore
                    else if (Directory.Exists(removeUnderscore(destinationPath)))
                    {
                        //Iterate until file doesn't exist
                        while(File.Exists(removeUnderscore(destinationPath) + name))
                        {
                            //Increment and add copy counter
                            copyCount++;
                            name = changeFileCount(name, copyCount);
                        }
                        //Do the copying
                        File.Copy(sourcePath, removeUnderscore(destinationPath) + name, true);
                        return "File " + name + " has been successfully coped to destination " + removeUnderscore(destinationPath);
                    }
                    else
                        return "Invalid Operation. File already exists at destination path";
                }
                //If source has a underscore
                else if (arguments[0].Contains('_'))
                {
                    //Remove the underscore
                    string newPath = removeUnderscore(sourcePath);
                    
                    //Check if the underscored source is a directory
                    if (isFile(newPath) == "directory")
                    {
                        string newDirectory = "";
                        //Checks if destination exists
                        if (Directory.Exists(destinationPath))
                        {
                            //Remove underscore and add name
                            newDirectory = removeUnderscore(destinationPath) + removeUnderscore(name);
                            
                            //Iterate until a directory doesn't exist
                            while (Directory.Exists(newDirectory))
                            {
                                //Increment copy counter
                                //Remove prev name, add new name
                                copyCount++;
                                newDirectory = newDirectory.Substring(0, newDirectory.LastIndexOf("/"));
                                newDirectory += removeUnderscore(name) + "_" + copyCount;
                            }
                            //Do the copying
                            copyDirectoryHelper(newPath, newDirectory);
                            return "Directory " + newDirectory.Substring(newDirectory.LastIndexOf('/')) + " has been successfully copied to destination " + destinationPath;
                        }
                        //If destination has a underscore
                        else if (Directory.Exists(removeUnderscore(destinationPath)))
                        {
                            //Remove underscore add name
                            newDirectory = removeUnderscore(destinationPath) + removeUnderscore(name);

                            //iterate until a directory doesn't exist
                            while (Directory.Exists(newDirectory))
                            {
                                //Increment copy counter, remove prev name, add new name
                                copyCount++;
                                newDirectory = newDirectory.Substring(0, newDirectory.LastIndexOf("/"));
                                newDirectory += removeUnderscore(name) + "_" + copyCount;
                            }
                            //Do the copying
                            copyDirectoryHelper(newPath, newDirectory);
                            return "Directory " + newDirectory.Substring(newDirectory.LastIndexOf('/')) + " has been successfully copied to destination " + destinationPath;
                        }
                        else
                            return "Invalid Operation. Directory doesn't exist at destination " + destinationPath;
                    }
                    //If the underscored source is a file
                    else if (isFile(newPath) == "file")
                    {
                        //Check if the destination exists
                        if (Directory.Exists(destinationPath))
                        {
                            //Add the name to the destination, remove underscore
                            destinationPath += removeUnderscore(name);

                            //Iterate until a file doesn't exist
                            while (File.Exists(destinationPath))
                            {
                                //Increment copy counter, change name
                                copyCount++;
                                destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));
                                name = changeFileCount(name, copyCount);
                                destinationPath += name;

                            }
                            //Do the copying
                            File.Copy(newPath, destinationPath, true);
                            return "File " + name + " has been successfully coped to destination " + destinationPath;
                        }
                        //Check if source has underscore and destination has underscore
                        else if (Directory.Exists(removeUnderscore(destinationPath)))
                        {
                            File.Copy(newPath, removeUnderscore(destinationPath), true);
                            return "File " + name + " has been successfully coped to destination " + destinationPath;
                        }
                        else return "Invalid Operation. File already exists at destination path.";
                    }
                    else
                        return "Invalid Operation. Source Directory doesn't exist.";

                }
                else return "Invalid Operation. Directory or File " + name + " is not found.";

            }
            //If arguments is 1, copying to default path
            else if (arguments.Length == 1)
            {
                //Globals
                int copyCount = 0;
                string sourcePath = Path.Combine(defaultPath,arguments[0]);
                string name = sourcePath.Substring(sourcePath.LastIndexOf('/'));
                
                //Checks if source is dir
                if (isFile(sourcePath) == "directory")
                {
                    //Set path
                    string directoryPath = sourcePath;
                    //Check if destination exists, iterate until it doesn't
                    while (Directory.Exists(directoryPath))
                    {
                        //Increment copy, change name
                        copyCount++;
                        directoryPath = directoryPath.Substring(0,directoryPath.LastIndexOf('/'));
                        directoryPath += name + "_" + copyCount;
                    }
                    //Do the copying
                    copyDirectoryHelper(sourcePath, directoryPath);
                    return "Directory " + name + " has been successfully copied to destination " + defaultPath;
                }
                //If source is file
                else if(isFile(sourcePath) == "file")
                {
                    //Iterate until file doesn't exist
                    while(File.Exists(defaultPath + name))
                    {
                        //Incremet copy, change name
                        copyCount++;
                        name = changeFileCount(name, copyCount);
                        
                    }
                    //Do the copying
                    File.Copy(sourcePath, defaultPath + name, true);
                    return "File " + name + " has been successfully copied to destination " + defaultPath;
                }
                //If default args contain spaces or underscores
                else if (arguments[0].Contains('_'))
                {
                    //Check if it is a dir
                    if (isFile(removeUnderscore(sourcePath)) == "directory")
                    {
                        //Remove underscore
                        string directoryPath = removeUnderscore(sourcePath);
                        //Iterate until dir doesn't exist
                        while (Directory.Exists(directoryPath))
                        {
                            //Increment copy counter, change name
                            copyCount++;
                            directoryPath = directoryPath.Substring(0, directoryPath.LastIndexOf("/"));
                            directoryPath += removeUnderscore(name) + "_" + copyCount;
                        }
                        //Do the copying
                        copyDirectoryHelper(removeUnderscore(sourcePath), directoryPath);
                        return "Directory " + directoryPath.Substring(directoryPath.LastIndexOf('/')) + " has been successfully copied to destination " + defaultPath;
                    }
                    //Checks if default source is a file with a underscore
                    else if (isFile(removeUnderscore(sourcePath)) == "file")
                    {
                        //Remove underscore
                        string destinationPath = removeUnderscore(sourcePath);

                        //Iterate until file doesn't exist
                        while (File.Exists(destinationPath))
                        {
                            //Increment copy counter, change name
                            copyCount++;
                            destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));
                            name = changeFileCount(name, copyCount);
                            destinationPath += name;

                        }
                        //Do the copying
                        File.Copy(removeUnderscore(sourcePath), destinationPath, true);
                        return "File " + name + " has been successfully coped to destination " + destinationPath;
                    }
                    else
                        return "Invalid Operation. Directory or File " + name + " is not found";
                }
                else
                    return "Invalid Operation. Directory or File " + name + " is not found.";
            }
            else
                return "Invalid Syntax. (copy 'sourcePath' 'destinationpath')";
        }


        //Function to rename files and directories
        //(ren 'sourcepath' 'newname')
        private static string renameFile(string sourcePath, string fileName)
        {
            string defaultPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string combinedPath = Path.Combine(defaultPath, sourcePath);


            if (File.Exists(combinedPath))
            {
                string reducedPath = combinedPath.Substring(0, combinedPath.LastIndexOf("/"));
                string newPath = Path.Combine(reducedPath, fileName);
                File.Move(combinedPath, newPath);
                return "File " + sourcePath + " has successfully been renamed to " + fileName + ".";
            }
            else if(Directory.Exists(combinedPath))
            {
                string reducedPath = combinedPath.Substring(0, combinedPath.LastIndexOf("/"));
                string newPath = Path.Combine(reducedPath, fileName);
                Directory.Move(combinedPath, newPath);
                return "Directory " + sourcePath + " has successfully been renamed to " + fileName + ".";
            }
            else if(combinedPath.Contains('_'))
            {
                combinedPath = removeUnderscore(combinedPath);
                if (File.Exists(combinedPath))
                {
                    string reducedPath = combinedPath.Substring(0, combinedPath.LastIndexOf("/"));
                    string newPath = Path.Combine(reducedPath, fileName);
                    File.Move(combinedPath, newPath);
                    return "File " + removeUnderscore(sourcePath) + " has successfully been renamed to " + fileName + ".";
                }
                else if (Directory.Exists(combinedPath))
                {
                    string reducedPath = combinedPath.Substring(0, combinedPath.LastIndexOf("/"));
                    string newPath = Path.Combine(reducedPath, fileName);
                    Directory.Move(combinedPath, newPath);
                    return "Directory " + removeUnderscore(sourcePath) + " has successfully been renamed to " + fileName + ".";
                }
                else
                    return "Invalid Operation. File or Directory " + sourcePath + " doesn't exist.";
            }
            else
                return "Invalid Operation. File " + sourcePath + " doesn't exist.";
        }

        //Rename file helper function
        private static string renameFileHelper(string[] arguments)
        {
            if (arguments.Length == 2)
            {
                string sourcePath = arguments[0];
                string fileName = arguments[1];

                return renameFile(sourcePath, fileName);
            }
            else
                return "Invalid Syntax. (ren 'sourcepath' 'newname')";
            
        }

        private static string deleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return "File successfully deleted at path " + filePath;
            }
            else
                return "Invalid Operation. File does not exist at path " + filePath;
                

           
        }

        private static string deleteFileHelper(string[] args)
        {
            string defaultPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            if (args.Length == 1)
            {
                string userInputPath = args[0];
                userInputPath = removeUnderscore(userInputPath);
                string filePath = Path.Combine(defaultPath, userInputPath);
                return deleteFile(filePath);
            }
            else if(args.Length > 1)
            {
                List<string> doesntExist = new List<string>();
                
                for (int i = 0; i < args.Length; i++)
                    args[i] = removeUnderscore(args[i]);

                foreach(string userInputPath in args)
                {
                    string filePath = Path.Combine(defaultPath, userInputPath);
                    if (!File.Exists(filePath))
                    {
                        doesntExist.Add(filePath.Substring(filePath.LastIndexOf('/')));
                    }
                        
                }

                if (doesntExist.Count > 0)
                    return "Invalid Operation. " + string.Join(',', doesntExist) + " doesn't exist.";
                else
                {
                    foreach(string filePath in args)
                        deleteFile(Path.Combine(defaultPath,filePath));
                    return "Files " + string.Join(',', args) + " have been successfully deleted.";
                }
            }
            else
                return "Invalid Syntax. (del 'filePath')";
        }
      
        //Method to change the title of the terminal window
        private static string changeTerminalTitle(string title, Context context, int appWidgetId)
        {
            widgetDataManager = new WidgetDataManager(context);
            widgetDataManager.saveTitle(appWidgetId,title);
            return "Successfully changed title to " + title;
        }

        //Helper function to change title of terminal window
        //Can use spaces with this one
        private static string changeTerminalTitleHelper(string[] args, Context context, int appWidgetId)
        {

            if (args.Length == 1)
            {
                string title = args[0];
                return changeTerminalTitle(title, context,appWidgetId);
            }
            else if(args.Length > 1)
            {
                string title = string.Join(' ', args);
                return changeTerminalTitle(title, context, appWidgetId);
            }
            else
                return "Invalid Syntax. (title 'name')";
        }

        //Method to change theme of terminal. Saves to SharedPreferences, loads on update
        private static string changeTerminalTheme(string[] args, Context context, int appWidgetId)
        {
            if (args.Length == 1)
            {
                string color = args[0].Trim();
                widgetDataManager = new WidgetDataManager(context);
                switch (color.ToLower())
                {
                    case "purple":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundPurple, Resource.Drawable.widgetbackGroundPurple, Resource.Drawable.terminalViewDrawBlack, "#000000");
                            return "Theme successfully changed to purple.";
                        }
                    case "yellow":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundYellow, Resource.Drawable.widgetbackGroundYellow, Resource.Drawable.terminalViewDraw, "#000000");
                            return "Theme successfully changed to yellow.";
                        }
                    case "black":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundBlack, Resource.Drawable.widgetbackGroundBlack, Resource.Drawable.terminalViewDrawBlack, "#52db02");
                            return "Theme successfully changed to black.";
                        }
                    case "red":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundRed, Resource.Drawable.widgetbackGroundRed, Resource.Drawable.terminalViewDrawBlack, "#000000");
                            return "Theme successfully changed to red.";
                        }
                    case "green":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundGreen, Resource.Drawable.widgetbackGroundGreen, Resource.Drawable.terminalViewDrawBlack, "#000000");
                            return "Theme successfully changed to green.";
                        }
                    case "pink":
                        {
                           widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundPink, Resource.Drawable.widgetbackGroundPink, Resource.Drawable.terminalViewDraw, "#000000");
                            return "Theme successfully changed to pink.";
                        }
                    case "orange":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGroundOrange, Resource.Drawable.widgetbackGroundOrange, Resource.Drawable.terminalViewDraw, "#000000");
                            return "Theme successfully changed to orange.";
                        }
                    case "default":
                        {
                            widgetDataManager.saveTheme(appWidgetId, Resource.Drawable.widgetbackGround, Resource.Drawable.widgetbackGround, Resource.Drawable.terminalViewDraw, "#000000");
                            return "Theme successfully reverted to default.";
                        }
                    default:
                        return "Invalid Operation. Color " + color + " does not exist.";
           
                }

            }
            else
                return "Invalid Syntax (color 'color')";
        }

        //Wipes Widget Data from SharedPreferences
        public static string wipeUserData(Context context, AppWidgetManager awm)
        {
            widgetDataManager = new WidgetDataManager(context);
            ComponentName appWidgetProvider = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetProvider)));
            int[] appWidgetIds = awm.GetAppWidgetIds(appWidgetProvider);
            widgetDataManager.wipeData(appWidgetIds);
            foreach(int appwidgetId in appWidgetIds)
            {
               widgetDataManager.checkInitData(appwidgetId);
            }
            return "Successfully wiped all widget data";
        }



        //Main function that takes a command, (string), and breaks it up to see what method to fire
        public static async Task<string> queryCommand(string commandText, Context context, AppWidgetManager appWidgetManager, int appWidgetId, RemoteViews view )
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
                            return await coinFlip(context,appWidgetManager,appWidgetId);
                        }
                    case "stordat":
                        {
                            return displayStorageInfo(context);
                        }
                    case "mkdir":
                        {
                            return createFolderHelper(arguments);
                        }
                    case "rmdir":
                        {
                            return deleteFolderHelper(arguments);
                        }
                    case "copy":
                        {
                            return copyHelper(arguments);
                        }
                    case "ren":
                        {
                            return renameFileHelper(arguments);
                        }
                    case "title":
                        {
                            return changeTerminalTitleHelper(arguments, context, appWidgetId);
                        }
                    case "theme":
                        {
                            return changeTerminalTheme(arguments,context,appWidgetId);
                        }
                    case "del":
                        {
                            return deleteFileHelper(arguments); 
                        }
                    case "wipewidgetdata":
                        {
                            return wipeUserData(context,appWidgetManager);
                        }

                    //Command List
                    case"help":
                        return printHelpHelper(arguments);

                    //No Command Found
                    default: return "Invalid Command";
                }
            }
            else
            {
                return "Invalid Command";
            }

            
        }

        
    }
}