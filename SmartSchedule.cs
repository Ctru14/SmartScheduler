using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public ApplicationDataContainer localData;
        public StorageFolder localFolder;
        public ApplicationDataCompositeValue scheduleStorageData;

        // Kets used to store/retrieve tasks from ApplicationData permanent storage entries
        public List<string> storageKeys;

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


        public SmartSchedule(uint id, string title, SolidColorBrush calColor)
        {
            // Create a new schedule - give it a title and store it in the filesystem!
            Title = title;
            taskSchedule = new Dictionary<DateTime, LinkedList<SmartTask>>();
            color = calColor;
            dateCreated = DateTime.Now;
            calID = id; // Randomly created
            numTasks = 0;

            // Store it
            // https://docs.microsoft.com/en-us/windows/uwp/design/app-settings/store-and-retrieve-app-data
            scheduleStorageData = new ApplicationDataCompositeValue
            {
                ["calID"] = calID,
                ["numTasks"] = numTasks,
                ["title"] = title,
                ["color"] = DataStorageTransformations.SolidColorBrush_ToStorageString(calColor),
                ["dateCreated"] = DataStorageTransformations.DateTime_ToStorageString(dateCreated)
            };

            
            
        }

        public void AddTask(SmartTask task)
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
            return "" + calID + "-" + (dateCreated.ToString("MM") + dateCreated.ToString("dd") + dateCreated.ToString("yyyy"))
                + "-" + (Title.Length > 8 ? Title.Substring(0, 9) : Title);
        }


    } // End of class definition


} // End of SmartScheduler namespace
