﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Services.Maps.LocalSearch;
using Windows.Storage;

namespace SmartScheduler
{
    public enum TaskType
    {
        TimedEvent,
        DeadlinedTask,
        FullDay
    }

    public enum YN { Yes = 1, No = 0 };

    public enum RepeatType
    {
        None,
        Daily,
        Weekly,
        MultiDayWeekly,
        Monthly,
        Yearly,
        Custom
    }


    public class SmartTask
    {
        public SmartTask(uint newID, SmartSchedule cal)
        {
            // Creating a new SmartTask object given its fields
            this.ID = newID;
            this.calendar = cal; // Why was this set to null..?

        }

        public SmartTask(ApplicationDataContainer tasksContainer, string key)
        {
            ApplicationDataCompositeValue taskComposite = null;
            try
            {
                taskComposite = (ApplicationDataCompositeValue)tasksContainer.Values[key];
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error: {e} could not load taskComposite value from key: {key}");
                this.title = null;
                return;
            }
            if (taskComposite == null)
            {
                Debug.WriteLine($"Task storage composite value in {tasksContainer.Name} was null for: {key}");
                Debug.WriteLine($"Printing all ({tasksContainer.Values.Count}) values:");
                foreach (object val in tasksContainer.Values)
                {
                    Debug.WriteLine(val);
                }
                this.title = null;
                return;
            }
            // Create a new SmartTask object from program data stored in memory
            try
            {
                this.ID = (uint)taskComposite["ID"];
                this.title = (string)taskComposite["title"];
                this.taskType = (TaskType)taskComposite["taskType"];
                this.repeatType = (RepeatType)taskComposite["repeatType"];
                //this.repeatDates = null; //TODO: do this - taskComposite["repeatDates"];
                this.when = DataStorageTransformations.DateTime_FromStorageString((string)taskComposite["when"]);
                this.duration = DataStorageTransformations.TimeSpan_FromStorageString((string)taskComposite["duration"]);
                this.timeRemaining = DataStorageTransformations.TimeSpan_FromStorageString((string)taskComposite["timeRemaining"]);
                this.required = (YN)taskComposite["required"];
                this.description = (string)taskComposite["description"];
                this.url = (string)taskComposite["url"];
                this.location = (string)taskComposite["location"];
                this.repeatNumTimes = taskComposite["repeatNumTimes"] == null ? 1 : (int)taskComposite["repeatNumTimes"];
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error when reading task from memory: {e}\nPrinting {taskComposite.Count} task values:");
                foreach (object val in taskComposite) Debug.WriteLine($"{val}");
            }
            this.SetRepeatDates();
        }

        public static readonly int[] hours = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                                              10,11,12,13,14,15,16,17,18,19,
                                              20,21,22,23,24 };

        public static readonly int[] mins = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        public readonly int[] hrs = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                                     10,11,12,13,14,15,16,17,18,19,
                                     20,21,22,23,24 };

        public readonly int[] mns = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        public readonly uint ID; // Global identifier for scheduled events

        public TaskType taskType { get; set; }

        public RepeatType repeatType { get; set; }

        public int repeatNumTimes { get; set; }

        public List<DateTime> repeatDates { get; set; }

        // Time arguments functions differently for events and tasks
        //   TimedEvent: when = start time, duration = duration of event
        //   DeadlinedTask: when = deadline, duration = time to complete, timeRemaining = how much left
        
        public string TimeString;

        private DateTime _when;
        public DateTime when
        {
            get { return _when; }
            set
            {
                _when = value;
                // Format the time of day string on a 12-hour cycle with AM/PM (hh:mm xM)
                int hourValue = ((when.Hour % 12) == 0) ? 12 : (when.Hour % 12);
                TimeString = (hourValue >= 10 ? "" : "  ") + hourValue + ":" + (when.Minute < 10 ? "0" : "") + when.Minute + ((when.Hour > 11) ? " PM" : " AM");
            }
        }

        public TimeSpan duration { get; set; }

        public TimeSpan timeRemaining { get; set; }

        public YN required { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public string location { get; set; }

        public string url { get; set; }

        public string descDisplay
        {
            get
            {
                string rtn = "";
                bool content = false;

                if (description.Length > 0)
                {
                    rtn += description;
                    content = true;
                }

                if (location.Length > 0)
                {
                    if (content)
                    {
                        rtn += "\n";
                    }
                    rtn += location;
                    content = true;
                }

                if (url.Length > 0)
                {
                    if (content)
                    {
                        rtn += "\n";
                    }
                    rtn += url;
                    content = true;
                }

                if (!content)
                {
                    //Debug.WriteLine($"SmartTask ({title}) event description display (descDisplay) empty, returning null");
                    rtn = null;
                }

                return rtn;
            }
        }

        public SmartSchedule calendar; // Reference to the calendar which owns the event

        // Data for storing a task in permanent storage
        public string StorageID()
        {
            // Format: "<ScheduleID>-<TaskID>-<MMDDYYYY(from 'when')>-<Up to first 8 chars of title>
            string s = "T-";
            s += calendar.calID + "-";
            s += ID + "-";
            s += when.ToString("MM") + when.ToString("dd") + when.ToString("yyyy");
            s += title.Length > 8 ? title.Substring(0,9) : title;
            return s;
        }

        public void StoreTaskData(ApplicationDataContainer tasksContainer)
        {
            ApplicationDataCompositeValue currentTaskComposite = new ApplicationDataCompositeValue();
            currentTaskComposite["ID"] = this.ID;
            currentTaskComposite["taskType"] = (int)this.taskType;
            currentTaskComposite["repeatType"] = (int)this.repeatType;
            currentTaskComposite["repeatNumTimes"] = this.repeatNumTimes;
            //currentTaskComposite["repeatDates"] = "";
            currentTaskComposite["when"] = DataStorageTransformations.DateTime_ToStorageString(this.when);
            currentTaskComposite["duration"] = DataStorageTransformations.TimeSpan_ToStorageString(this.duration);
            currentTaskComposite["timeRemaining"] = DataStorageTransformations.TimeSpan_ToStorageString(this.timeRemaining);
            currentTaskComposite["required"] = (int)this.required;
            currentTaskComposite["title"] = this.title;
            currentTaskComposite["description"] = this.description;
            currentTaskComposite["location"] = this.location;
            currentTaskComposite["url"] = this.url;
            currentTaskComposite["calendar"] = this.calendar.StorageID();
            tasksContainer.Values[StorageID()] = currentTaskComposite;
        }

        public void SetRepeatDates()
        {
            DateTime date = this.when;
            this.repeatDates = new List<DateTime>();

            if (this.repeatNumTimes < 1)
            {
                repeatNumTimes = 1;
            }

            switch(this.repeatType)
            {
                case RepeatType.Daily:
                    //Debug.WriteLine($"Generating daily repeat for task {ID}:");
                    for (int i = 0; i < this.repeatNumTimes; ++i)
                    {
                        repeatDates.Add(date);
                        date = date.AddDays(1);
                    }
                    break;
                case RepeatType.Weekly:
                    //Debug.WriteLine($"Generating weekly repeat for task {ID}:");
                    for (int i = 0; i < this.repeatNumTimes; ++i)
                    {
                        repeatDates.Add(date);
                        date = date.AddDays(7);
                    }
                    break;
                case RepeatType.Monthly:
                    //Debug.WriteLine($"Generating monthly repeat for task {ID}:");
                    for (int i = 0; i < this.repeatNumTimes; ++i)
                    {
                        repeatDates.Add(date);
                        date = date.AddMonths(1);
                    }
                    Debug.WriteLine(repeatDates);
                    break;
                case RepeatType.Yearly:
                    //Debug.WriteLine($"Generating yearly repeat for task {ID}:");
                    for (int i = 0; i < this.repeatNumTimes; ++i)
                    {
                        repeatDates.Add(date);
                        date = date.AddYears(1);
                    }
                    break;
                case RepeatType.None:
                    //Debug.WriteLine($"Generating None repeat for task {ID}:");
                    repeatDates.Add(date);
                    break;
                default:
                    Debug.WriteLine($"[Unfinished!] Repeat Type {this.repeatType} not yet supported: defaulting to no repeat");
                    repeatDates.Add(date);
                    break;
            }
        }

        
        public override string ToString()
        {
            string str = this.taskType.ToString() + ":  ";
            str += this.title;
            str += " " + this.when.ToString("g");
            if (taskType == TaskType.TimedEvent)
            {
                str += " Dur: " + this.duration;
            }
            return str;
        }

        public void DeleteTask()
        {
            Debug.WriteLine($"Deleting task {StorageID()} from within task class");

            // Remove task's data from calendar and storage
            if (!calendar.DeleteTask(this))
            {
                Debug.WriteLine($"Error deleting task {StorageID()} from {calendar.StorageID()} - see above messages");
            }
        }

        ~SmartTask()
        {
            Debug.WriteLine($"Destructor called for task {this.ID}");
        }

    }
}
