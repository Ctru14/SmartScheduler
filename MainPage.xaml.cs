﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SmartScheduler
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Global MainPage variables
        public SmartSchedule schedule;
        public uint currentNumTasksInSchedule = 0;
        public uint nextEventID = 1;
        public Random rng = new Random();

        // Storage system variables
        public ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public ApplicationDataContainer startupSettings;
        public ApplicationDataContainer scheduleData;
        public StorageFolder taskDataContainer = ApplicationData.Current.LocalFolder;
        public string scheduleStorageID;

        private DateTime _selectedDate;
        public DateTime selectedDate {
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                TB_DaySchedule.Text = "Schedule: " + selectedDate.ToString("d");
                //syncScheduleViewer();
            }
        }
        public MainPage()
        {
            bool opened = true;
            this.InitializeComponent();

            // TEMP: delete settings - for debugging only
            //localSettings.DeleteContainer("startup");

            // Try to read composite value "startup" from settings to either load or create data
            try
            {
                startupSettings = localSettings.CreateContainer("startup", ApplicationDataCreateDisposition.Existing);
            }
            catch (Exception e)
            {
                // Startup settings have never been created! Create a new Schedule data structure
                System.Diagnostics.Debug.WriteLine("Creating ApplicationData in localSettings for new schedule");

                opened = false;
                startupSettings = localSettings.CreateContainer("startup", ApplicationDataCreateDisposition.Always);

                // Create new scjedule object
                nextEventID = 1; // TODO: read from global configuration file (permanent storage)
                uint calID = (uint)rng.Next(int.MinValue, int.MaxValue);
                schedule = new SmartSchedule(startupSettings, calID, "Default", new SolidColorBrush(Color.FromArgb(0xFF, 0x10, 0xEE, 0xEE)));
                scheduleStorageID = schedule.StorageID();

                // Store these values into the startup settings
             //   startupSettings = new ApplicationDataCompositeValue();
                startupSettings.Values["nextEventID"] = nextEventID; // Events always start at 1
                startupSettings.Values["scheduleStorageID"] = scheduleStorageID;
                scheduleData = startupSettings.CreateContainer(scheduleStorageID, ApplicationDataCreateDisposition.Always);
                 //startupSettings.Values[scheduleStorageID] = scheduleData;
                 //localSettings.Values["startup"] = startupSettings;
                
                    
            }
            if (opened)
            {
                // Startup settings already exist! Load previous values into usable data structure
                System.Diagnostics.Debug.WriteLine("Loading schedule data from application storage");
                nextEventID = (uint)startupSettings.Values["nextEventID"];
                scheduleStorageID = (string)startupSettings.Values["scheduleStorageID"];
                scheduleData = startupSettings.CreateContainer(scheduleStorageID, ApplicationDataCreateDisposition.Existing);

                schedule = new SmartSchedule(startupSettings, scheduleStorageID);
            }
            
            // Initialize XAML components
            CB_TypePicker.ItemsSource = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToList();
            CB_RequiredPicker.ItemsSource = Enum.GetValues(typeof(YN)).Cast<YN>().ToList();
            CB_RepeatPicker.ItemsSource = Enum.GetValues(typeof(RepeatType)).Cast<RepeatType>().ToList();
            CB_DurHoursPicker.ItemsSource = SmartTask.hours;
            CB_DurMinsPicker.ItemsSource = SmartTask.mins;
            LV_Schedule.ItemsSource = schedule.GetTastsAt(selectedDate.Date);
            selectedDate = DateTime.Now;

        }

        private void CV_MainCalendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (args.AddedDates.Count > 0)
            {
                selectedDate = args.AddedDates.First().Date;
                CDP_NewItemDate.Date = args.AddedDates.First().Date;
            }
            LV_Schedule.ItemsSource = schedule.GetTastsAt(selectedDate);
        }

        private void PB_AddToSchedule_Click(object sender, RoutedEventArgs e)
        {
            bool filled = true;
            // Check to see if required fields are filled in:

            // Title
            if (TB_NewTitle.Text.Length == 0)
            {
                filled = false;
                TB_AsteriskTitle.Text = "*";
            } 
            else 
                TB_AsteriskTitle.Text = ""; 

            // Date
            if (!CDP_NewItemDate.Date.HasValue)
            {
                filled = false;
                TB_AsteriskDate.Text = "*";
            } else 
                TB_AsteriskDate.Text = "";

            // Task type
            if (CB_TypePicker.SelectedItem == null)
            {
                filled = false;
                TB_AsteriskType.Text = "*";
            }
            else
                TB_AsteriskType.Text = "";

            // Duration is not required for a FullDay event
            if ((TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay && CB_DurHoursPicker == null)
            {
                filled = false;
                TB_AsteriskDuration.Text = "*";
            }
            else
                TB_AsteriskDuration.Text = "";

            // Duration is not required for a FullDay event
            if ((TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay && CB_DurMinsPicker == null)
            {
                filled = false;
                TB_AsteriskDuration.Text = "*";
            }
            else
                TB_AsteriskDuration.Text = "";

            // Time
            if ((TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay && !TP_NewItemTime.SelectedTime.HasValue)
            {
                filled = false;
                TB_AsteriskTime.Text = "*";
            }
            else
                TB_AsteriskTime.Text = "";

            // Required?
            if (CB_RequiredPicker.SelectedValue == null)
            {
                filled = false;
                TB_AsteriskRequired.Text = "*";
            }
            else
                TB_AsteriskRequired.Text = "";

            // Repeat
            if (CB_RepeatPicker.SelectedValue == null)
            {
                filled = false;
                TB_AsteriskRepeat.Text = "*";
            }
            else
                TB_AsteriskRepeat.Text = "";


            if (!filled)
            {
                TB_RequiredFields.Text = "The marked(*) fields are required.";
                return;
            }
            // Remove failed text markers from UI
            TB_RequiredFields.Text = "";

            // Create SmartTask object out of the text fields
            SmartTask newTask = new SmartTask(nextEventID++, schedule);

            // Get full DateTime from composite boxes
            DateTimeOffset date = CDP_NewItemDate.Date.Value;
            int hour = TP_NewItemTime.SelectedTime.Value.Hours;
            int minute = TP_NewItemTime.SelectedTime.Value.Minutes;
            DateTime when = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Get duration
            int durHour = (int)CB_DurHoursPicker.SelectedItem;
            int durMinute = (int)CB_DurMinsPicker.SelectedItem;
            newTask.taskType = (TaskType) CB_TypePicker.SelectedValue;
            newTask.repeatType = (RepeatType) CB_RepeatPicker.SelectedValue;
            newTask.when = when;
            newTask.title = TB_NewTitle.Text;
            newTask.description = TB_NewDescription.Text;
            newTask.required = (YN) CB_RequiredPicker.SelectedValue;
            newTask.timeRemaining = newTask.duration = new TimeSpan(durHour, durMinute, 0);
            newTask.url = TB_NewURL.Text;

            // Add to global schedule variable
            schedule.AddTask(newTask);


            if (selectedDate.Date == when.Date)
            {
                // TODO: Update the ListView viewer when adding a new task to the current date
                LV_Schedule.ItemsSource = null;
                LV_Schedule.ItemsSource = schedule.GetTastsAt(selectedDate);
            }

        }


/*        public void syncScheduleViewer()
        {
            // Compare the number of events added to the schedule view to the selected day
            LinkedList<SmartTask> taskList;
            if (schedule.taskSchedule.TryGetValue(selectedDate, out taskList) && taskList.Count > 0)
            {
                // Some tasks exist for that day - see how many
                if (currentNumTasksInSchedule != taskList.Count)
                {
                    // Tasks don't match! - update list view to reflect the schedule
                    
                    // Check each item in the taskList to see if it exists in the viewer
                    for (int i = 0; i < taskList.Count; ++i)
                    {
                        // Check the ListView for this item, insert it to LV if it doesn't exist
                        if (!LV_Schedule.Items.Contains(taskList.ElementAt(i)))
                        {
                            LV_Schedule.Items.Add(taskList.ElementAt(i));
                        }
                    }

                    // Check each item in the ListView to see if it exists in the taskList
                    for (int i = 0; i < LV_Schedule.Items.Count; ++i)
                    {
                        // Check the taskList for this item, delete it from LV it doesn't exist
                        if (!taskList.Contains(LV_Schedule.Items.ElementAt(i)))
                        {
                            LV_Schedule.Items.Remove(i);
                        }
                    }

                    // Sort ListView by descending date
                    LV_Schedule.Items.OrderBy(task => ((SmartTask)task).when);
                }
            }
            else
            {
                // There are no tasks for the selected day! Clear the list view
                for (int i = LV_Schedule.Items.Count - 1; i >= 0; --i)
                {
                    LV_Schedule.Items.RemoveAt(i);
                }
            }
            
        } */


    } // End of namespace


}
