using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
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

        public List<TaskType> taskTypeList = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToList();
        public List<YN> ynList = Enum.GetValues(typeof(YN)).Cast<YN>().ToList();
        public List<RepeatType> repeatTypeList = Enum.GetValues(typeof(RepeatType)).Cast<RepeatType>().ToList();
        
        // Storage variables
        //        public ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public ApplicationDataContainer startupSettings;
        public ApplicationDataContainer scheduleData;
        public ApplicationDataContainer tasksContainer;
        public StorageFolder localFolder;
        public ApplicationDataCompositeValue scheduleStorageData; // DEPRECATED - use a container instead

        // Kets used to store/retrieve tasks from ApplicationData permanent storage entries
        public List<string> storageKeys;
        public string storageKeysString // Use ` as delimiter
        {
            get
            {
                string keys = "";
                foreach (string s in storageKeys.ToArray())
                {
                    keys += s + "`";
                }
                if (keys.Length > 0)
                {
                    keys = keys.Substring(0, keys.Length - 1);
                }
                return keys;
            }
        }

        private string _Title = null;
        public string Title {
            get { return _Title; }
            set
            {
                if (_Title != null) 
                {
                    // TODO - if title changes, update the app data storage string
                }
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
            storageKeys = new List<string>(); // Stored as one continuous string separated by ` in memory
            startupSettings = container;

            // Store the new schedule data in the application storage
            scheduleData = startupSettings.CreateContainer(StorageID(), ApplicationDataCreateDisposition.Always);

            // Add values to the app data storage
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

        // Creates and returns the SmartTask object in the respected schedule, but DOES NOT ADD IT TO THE SCHEDULE
        public SmartTask CreateTask(uint eventID, DateTimeOffset date, int hour, int minute, int durHour, int durMinute, TaskType taskType, RepeatType repeatType, int repeatNumTimes, string title, string description, YN required, string location, string url)
        {
            // Create SmartTask object out of the text fields
            SmartTask newTask = new SmartTask(eventID, this);

            // Get full DateTime from composite boxes
            DateTime when = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Get duration
            newTask.taskType = taskType;
            newTask.repeatType = repeatType;
            newTask.repeatNumTimes = repeatNumTimes;
            newTask.when = when;
            newTask.title = title;
            newTask.description = description;
            newTask.required = required;
            newTask.timeRemaining = newTask.duration = new TimeSpan(durHour, durMinute, 0);
            newTask.location = location;
            newTask.url = url;

            newTask.SetRepeatDates();

            // Creates the SmartTask item
            return newTask;
        }

        public void AddTask(SmartTask task, bool newToStore = true)
        {
            LinkedList<SmartTask> taskList;
            //DateTime date = task.when.Date;
            task.calendar = this;
            
            foreach (DateTime date in task.repeatDates)
            {
                // Add new task - inset into LinkedList 
                if (taskSchedule.TryGetValue(date.Date, out taskList))
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
                    taskSchedule.Add(date.Date, taskList);
                }
            }
            numTasks++;


            // Store task in application data storage
            if (newToStore)
            {
                // Update the storage keys string
                string storageKey = task.StorageID();
                storageKeys.Add(storageKey);
                scheduleData.Values["storageKeys"] = storageKeysString;
                task.StoreTaskData(tasksContainer);
            }
        }

        public LinkedList<SmartTask> GetTastsAt(DateTime date)
        {
            LinkedList<SmartTask> taskList;
            if (this.taskSchedule.TryGetValue(date.Date, out taskList))
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

        public bool DeleteTask(SmartTask task) // Deletes a task from app memory then removes it from the SmartSchedule data structure
        {
            bool status = true; // Return value for whether or not it succeeded
            string taskID = task.StorageID();
            string scheduleID = this.StorageID();

            Debug.WriteLine($"Deleting task: {taskID} from schedule {scheduleID}...");

            // Remove task from SmartSchedule - return value represents whether or not the remove was successful
            status &= removeTaskFromSchedule(task);

            // Retrieve the data from application memory (note: open existing)
            scheduleData = startupSettings.CreateContainer(scheduleID, ApplicationDataCreateDisposition.Existing);

            // Remove task key from storage keys
            if (storageKeys.Contains(taskID))
            {
                status &= storageKeys.Remove(taskID);
            }
            else
            {
                Debug.WriteLine($"Unexpected error: {taskID} not found in storage keys!");
                status = false;
            }
            status &= scheduleData.Values.Remove("storageKeys");
            scheduleData.Values["storageKeys"] = storageKeysString;

            // Remove task from app storage
            try
            {
                tasksContainer.DeleteContainer(taskID);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in task delete from SmartSchedule: Task container {taskID} was not found.\nException message: {e.Message}");
                status = false;
            }

            // Update the UI to reflect a new task added to the schedule
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(main_LV_schedule.ItemsSource)));
           
            return status;

        }

        public bool removeTaskFromSchedule(SmartTask task) // Returns: T/F on whether or not the remove was successful
        {
            LinkedList<SmartTask> taskList;
            //DateTime date = task.when.Date;
            bool rtn = true;
            foreach (DateTime date in task.repeatDates)
            {
                // Add new task - inset into LinkedList 
                if (taskSchedule.TryGetValue(date.Date, out taskList))
                {
                    taskList.Remove(task);

                    // Check if that was the only task for that date - if so, remove the list
                    if (taskList.Count == 0)
                    {
                        if (!taskSchedule.Remove(date)) Debug.WriteLine($"Error removing empty schedule at date {date}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Remove Task Error: Task list not found for date {date}");
                    rtn = false;
                }
            }
            return rtn;

        }

        public string StorageID()
        {
            // Format: <calID>-<MMDDYYYY(from 'dateCreated')>-<Title up to 8 chars>
            return "C-" + calID + "-" + (dateCreated.ToString("MM") + dateCreated.ToString("dd") + dateCreated.ToString("yyyy"))
                + "-" + (Title.Length > 8 ? Title.Substring(0, 9) : Title);
        }


    } // End of SmartSchedule class definition


} // End of SmartScheduler namespace
