using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps.LocalSearch;

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
        public SmartTask(int newID)
        {
            this.ID = newID;
        }

        public static readonly int[] hours = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                                              10,11,12,13,14,15,16,17,18,19,
                                              20,21,22,23,24 };

        public static readonly int[] mins = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        public readonly int ID; // Global identifier for scheduled events

        public TaskType taskType { get; set; }

        public RepeatType repeatType { get; set; }

        public List<DateTime> customRepeat { get; set; }

        // Time arguments functions differently for events and tasks
        //   TimedEvent: when = start time, duration = duration of event
        //   DeadlinedTask: when = deadline, duration = time to complete, timeRemaining = how much left

        public DateTime when { get; set; }

        public TimeSpan duration { get; set; }

        public TimeSpan timeRemaining { get; set; }

        public YN required { get; set; }

        public string title { get; set; }
        
        public string description { get; set; }

        public string url { get; set; }

        public override string ToString()
        {
            string str = this.taskType.ToString() + ":  ";
            str += this.title;
            str += (" " + this.when.ToString("g"));
            if (taskType == TaskType.TimedEvent) str += (" Dur: " + this.duration);
            return str;
        }


    }
}
