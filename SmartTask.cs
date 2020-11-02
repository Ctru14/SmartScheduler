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
        public static readonly int[] hours = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                                              10,11,12,13,14,15,16,17,18,19,
                                              20,21,22,23,24 };

        public static readonly int[] mins = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        TaskType taskType;

        RepeatType repeatType;

        List<DateTime> customRepeat;

        // Time arguments functions differently for events and tasks
        //   TimedEvent: when = start time, duration = duration of event
        //   DeadlinedTask: when = deadline, duration = time to complete, timeRemaining = how much left
        DateTime when; 
        TimeSpan duration;
        TimeSpan timeRemaining;

        YN required;

        String title;
        String description;

        String url;

    }
}
