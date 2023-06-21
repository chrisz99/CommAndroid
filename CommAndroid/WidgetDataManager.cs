using Android.Content;
using System.Runtime.ConstrainedExecution;
using Newtonsoft.Json;
using Android.Preferences;
using AndroidX.Preference;
using System.Collections.Generic;
using AndroidX.AppCompat.View.Menu;
using System.Linq;

namespace CommAndroid
{
    //Class to handle command/result data in the lists of the widgets
    //Uses SharedPreferences to store and retrive the data
    //Uses Newtonsoft.Json to convert list objects to json
    class WidgetDataManager
    {
        //Global Vars
        private const string PREF_NAME = "WidgetData";
        private const string KEY_PREFIX_COMMANDS = "commands_";
        private const string KEY_PREFIX_RESULTS = "results_";
        private const string KEY_PREFIX_THEME = "theme_";

        private ISharedPreferences sharedPreferences;
        private JsonSerializerSettings jsonSerializerSettings;

        //Constructor
        public WidgetDataManager(Context context)
        {
            sharedPreferences = AndroidX.Preference.PreferenceManager.GetDefaultSharedPreferences(context);
            jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        }

        //Checks if WidgetData exists, if not => create widget data with key appWidgetId
        public void checkInitData(int appWidgetId)
        {
            if (!sharedPreferences.Contains(KEY_PREFIX_COMMANDS + appWidgetId))
            {
                List<string> commandList = new List<string>();
                List<string> resultList = new List<string>();
                commandList.Add("CMD: ");
                resultList.Add("");
                saveTheme(appWidgetId, Resource.Drawable.widgetbackGround, Resource.Drawable.widgetbackGround, Resource.Drawable.terminalViewDraw, "#000000");
                saveLists(appWidgetId, commandList, resultList);
            }
        }

        //Saves theme to SharedPreferences. (Saves as resource id)
        public void saveTheme(int appWidgetId, int widgetResourceId, int inputBackgroundResourceId, int inputBoxBackgroundResourceId, string color)
        {
            string themeKey = KEY_PREFIX_THEME + appWidgetId;

            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            editor.PutString(themeKey, widgetResourceId + "_" + inputBackgroundResourceId + "_" + inputBoxBackgroundResourceId + "_" + color);
            editor.Apply();
        }

        //Grabs a theme from SharedPreferences by AppWidgetId
        public string getTheme(int appWidgetId)
        {
            string themeKey = KEY_PREFIX_THEME + appWidgetId;
            string theme = sharedPreferences.GetString(themeKey, null);
            return theme;
        }

        //Method to save lists to shared preferences
        public void saveLists(int appWidgetId, List<string> commands, List<string> results)
        {
            string commandKey = KEY_PREFIX_COMMANDS + appWidgetId;
            string resultKey = KEY_PREFIX_RESULTS + appWidgetId;

            string commandsJson = JsonConvert.SerializeObject(commands, jsonSerializerSettings);
            string resultsJson = JsonConvert.SerializeObject(results,jsonSerializerSettings);

            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            editor.PutString(commandKey, commandsJson);
            editor.PutString(resultKey, resultsJson);
            editor.Apply();
        }

        //Method to remove last command/result from list
        public void removeLast(int appWidgetId)
        {
            List<string> commands = getCommands(appWidgetId);
            List<string> results = getResults(appWidgetId);
            if (commands.Count > 0 && results.Count > 0)
            {
                commands.RemoveAt(commands.Count - 1);
                results.RemoveAt(results.Count - 1);
            }
            saveLists(appWidgetId, commands, results);
        } 

        public void wipeData(int[] appWidgetIds)
        {
            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            IDictionary<string, object> allPreferences = sharedPreferences.All;

            foreach (string key in allPreferences.Keys) {
                foreach(int appWidgetId in appWidgetIds)
                {
                    if (!key.Contains(appWidgetId.ToString()))
                        editor.Remove(key);
                }
            
            }
            editor.Apply();
        }

        //Method to grab commands from SharedPreferences
        public List<string> getCommands(int appWidgetId)
        {
            string commandKey = KEY_PREFIX_COMMANDS + appWidgetId;
            string commandsJson = sharedPreferences.GetString(commandKey, null);

            if(commandsJson != null) 
            return JsonConvert.DeserializeObject<List<string>>(commandsJson,jsonSerializerSettings);
            else return new List<string>();
        }

        //Method to grab results from SharedPreferences
        public List<string> getResults(int appWidgetId)
        {
            string resultKey = KEY_PREFIX_RESULTS + appWidgetId;
            string resultsJson = sharedPreferences.GetString(resultKey, null);

            if(resultsJson != null)
            return JsonConvert.DeserializeObject<List<string>>(resultsJson,jsonSerializerSettings);
            else return new List<string>();
        }

        //Method to add commands/results to list to SharedPreferences
        public void addCommand(string command, string result, int appWidgetId)
        {
     

            removeLast(appWidgetId);

            List<string> commandList = getCommands(appWidgetId);
            List<string> resultsList = getResults(appWidgetId);

            commandList.Add("CMD: " + command);
            resultsList.Add(result);
            commandList.Add("CMD: ");
            resultsList.Add("");

            saveLists(appWidgetId,commandList,resultsList);
        }

        //Method to remove lists from SharedPreferences
        public void removeLists(int appWidgetId)
        {
            if (sharedPreferences.Contains(KEY_PREFIX_COMMANDS + appWidgetId))
            {
                ISharedPreferencesEditor editor = sharedPreferences.Edit();
                editor.Remove(KEY_PREFIX_COMMANDS + appWidgetId);
                editor.Remove(KEY_PREFIX_RESULTS + appWidgetId);
                editor.Apply();
            }
        }

        //Method to clear the data in list in SharedPreferences
        public void clearList(int appWidgetId)
        {
            List<string> commandList = getCommands(appWidgetId);
            List<string> resultsList = getResults(appWidgetId);

            commandList.Clear();
            resultsList.Clear();
            commandList.Add("CMD: ");
            resultsList.Add("");

            saveLists(appWidgetId, commandList, resultsList);
        }

        //Method to get count of commands in SharedPreferences
        public int getCount(int appWidgetId)
        {
            if (sharedPreferences.Contains(KEY_PREFIX_COMMANDS + appWidgetId))
            {
                List<string> commandList = getCommands(appWidgetId);
                return commandList.Count - 1;
            }
            else
                return 0;
        }




    }
}