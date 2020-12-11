using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Storage;

namespace SmartScheduler
{
    public class SmartSchedule
    {
        public Dictionary<DateTime, LinkedList<SmartTask>> taskSchedule;

        public SolidColorBrush color { get; set; }

        // Each schedule (instance of this class) has its own ID# stored at the beginning of its file
        public readonly uint calID;

        private readonly DateTime dateCreated;

        public uint numTasks;

        // Storage variables
        //        public ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public ApplicationDataContainer startupSettings;
        public ApplicationDataContainer scheduleData;
        public ApplicationDataContainer tasksContainer;
        public StorageFolder localFolder;
        public ApplicationDataCompositeValue scheduleStorageData; // DEPRECATED - use a container instead

        // Kets used to store/retrieve tasks from ApplicationData permanent storage entries
        public List<string> storageKeys;
        public string storageKeysString = ""; // Use ` as delimiter

        private string _Title = null;
        public string Title {
            get { return _Title; }
            set
            {
                // TODO - if title changes, update the app data storage string
                if (_Title != null) {}
                _Title = value;
            }
        }


        public SmartSchedule(ApplicationDataContainer container, uint id, string title, SolidColorBrush calColor)
        {
            // Create a new schedule - give it a title and store it in the filesystem!
            Title = title;
            taskSchedule = new Dictionary<DateTime, LinkedList<SmartTask>>();
            color = calColor;
            dateCreated = DateTime.Now;
            calID = id; // Randomly created 
            numTasks = 0;
            storageKeys = new List<string>(); // Stored as one continuous string separated by spaces in memory
            startupSettings = container;

            // Store the new schedule data in the application storage
            scheduleData = startupSettings.CreateContainer(storageID(), ApplicationDataCreateDisposition.Always);

            // Add values to the 
            scheduleData.Values["calID"] = calID;
            scheduleData.Values["numTasks"] = numTasks;
            scheduleData.Values["title"] = Title;
            scheduleData.Values["color"] =  DataStorageTransformations.SolidColorBrush_ToStorageString(color);
            scheduleData.Values["dateCreated"] = DataStorageTransformations.DateTime_ToStorageString(dateCreated);
            scheduleData.Values["storageKeys"] = "";
            tasksContainer = scheduleData.CreateContainer("tasks", ApplicationDataCreateDisposition.Always);
        }

        public SmartSchedule(ApplicationDataContainer container, string scheduleID)
        {
            // Load an existing schedule from application data storage given its ID string
            taskSchedule = new Dictionary<DateTime, LinkedList<SmartTask>>();
            startupSettings = container;
            try
            {
                // Retrieve the data from application memory (note: open existing)
                scheduleData = startupSettings.CreateContainer(scheduleID, ApplicationDataCreateDisposition.Existing);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Error {e.Message}: Schedule at {scheduleID} does not exist in local storage!");
                return;
            }
            // Fill in data from the container
            calID = (uint)scheduleData.Values["calID"];
            numTasks = (uint)scheduleData.Values["numTasks"];
            Title = (string)scheduleData.Values["title"];
            color = DataStorageTransformations.SolidColorBrush_FromStorageString((string)scheduleData.Values["color"]);
            dateCreated = DataStorageTransformations.DateTime_FromStorageString((string)scheduleData.Values["dateCreated"]);
            tasksContainer = scheduleData.CreateContainer("tasks", ApplicationDataCreateDisposition.Existing);

            // Store and load tasks from memory!
            string taskKeys = (string)scheduleData.Values["storageKeys"];
            if (taskKeys.Length > 0)
            {
                // Only try to add tasks if a key exists
                storageKeys = taskKeys.Split('`').ToList<string>();
                taskSchedule = new Dictionary<DateTime, LinkedList<SmartTask>>();
                foreach (string key in storageKeys.ToList())
                {
                    if (key.Length > 0)
                    {
                        SmartTask newTask = new SmartTask(tasksContainer, key);
                        if (newTask.title != null)
                        {
                            this.AddTask(newTask, false);
                        }
                    }
                }
            }
            else
            {
                storageKeys = new List<string>(); // Stored as one continuous string separated by spaces in memory
            }

        }

        public void AddTask(SmartTask task, bool newToStore = true)
        {
            LinkedList<SmartTask> taskList;
            DateTime date = task.when.Date;
            task.calendar = this;

            // Add new task - inset into LinkedList 
            if (taskSchedule.TryGetValue(date, out taskList))
            {
                // Schedule already exists for this day -> add event
                // Find value in list that it should be inserted after

                // Check edge case: first and last
                if (task.when < taskList.First.Value.when)
                {
                    // Insert first
                    taskList.AddFirst(task);
                } 
                else if (task.when >= taskList.Last.Value.when)
                {
                    // Insert last
                    taskList.AddLast(task);
                } 
                else
                {
                    // Somewhere in the middle
                    var tasks = taskList.GetEnumerator();
                    LinkedListNode<SmartTask> prev = taskList.First;
                    LinkedListNode<SmartTask> next = taskList.First;
                    //  Loop until times of prev < task < next
                    do
                    {
                        prev = next;
                        if (next == null)
                        {
                            // You've reached the end of the list!
                            break;
                        }
                        next = next.Next;

                    } while (!(prev.Value.when < task.when && task.when < next.Value.when));
                    taskList.AddAfter(prev, task);
                }
                                
            } 
            else
            {
                // This day does not yet exist in our schedule -> create it
                taskList = new LinkedList<SmartTask>();
                taskList.AddFirst(task);
                taskSchedule.Add(date, taskList);
            }
            numTasks++;


            // Update the storage keys string
            string storageKey = task.storageID();
            this.storageKeysString += (storageKey + "`");
            storageKeys.Add(storageKey);

            // Store task in application data storage
            if (newToStore)
            {
                scheduleData.Values["storageKeys"] = storageKeysString;
                task.storeTaskData(tasksContainer);
            }
        }

        public LinkedList<SmartTask> GetTastsAt(DateTime date)
        {
            LinkedList<SmartTask> taskList;
            if (this.taskSchedule.TryGetValue(date, out taskList))
            {
                return taskList;
            }
            else
            {
                return null;
            }
        }

        public int GetTaskCount(DateTime date)
        {
            LinkedList<SmartTask> taskList = GetTastsAt(date.Date);
            if (taskList != null)
            {
                return taskList.Count;
            }
            else
            {
                return 0;
            }
        }

        public string storageID()
        {
            // Format: <calID>-<MMDDYYYY(from 'dateCreated')>-<Title up to 8 chars>
            return "C-" + calID + "-" + (dateCreated.ToString("MM") + dateCreated.ToString("dd") + dateCreated.ToString("yyyy"))
                + "-" + (Title.Length > 8 ? Title.Substring(0, 9) : Title);
        }


    } // End of class definition


} // End of SmartScheduler namespace
