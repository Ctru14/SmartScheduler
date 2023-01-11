using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
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
            catch //(Exception e)
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
            CB_TypePicker.ItemsSource = schedule.taskTypeList;
            CB_RequiredPicker.ItemsSource = schedule.ynList;
            CB_RepeatPicker.ItemsSource = schedule.repeatTypeList;
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

            // Get full DateTime from composite boxes
            DateTimeOffset date = CDP_NewItemDate.Date.Value;
            int hour = TP_NewItemTime.SelectedTime.Value.Hours;
            int minute = TP_NewItemTime.SelectedTime.Value.Minutes;
            DateTime when = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Get duration
            int durHour = (int)CB_DurHoursPicker.SelectedItem;
            int durMinute = (int)CB_DurMinsPicker.SelectedItem;
            TaskType taskType = (TaskType) CB_TypePicker.SelectedValue;
            RepeatType repeatType = (RepeatType) CB_RepeatPicker.SelectedValue;
            YN required = (YN) CB_RequiredPicker.SelectedValue;

            // Create a new task from the schedule
            SmartTask newTask = schedule.CreateTask(nextEventID++, date, hour, minute, durHour, durMinute, taskType, repeatType, TB_NewTitle.Text, TB_NewDescription.Text, required, TB_NewURL.Text);

            // Add to global schedule variable
            schedule.AddTask(newTask);

            if (selectedDate.Date == when.Date)
            {
                //Update the ListView viewer when adding a new task to the current date
                RefreshScheduleView();
            }

        }

        private void PB_DeleteSelectedTask(object sender, RoutedEventArgs e)
        {
            // Deletes the task whose flyout menu was selected
            ((SmartTask)((Button)sender).DataContext).DeleteTask();

            // Refreshes the schedule viewer display to remove the task
            RefreshScheduleView();
        }

        private void PB_EditFlyoutShowFlyout(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void PB_UpdateSelectedTask(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Updating task: {(SmartTask)((Button)sender).DataContext}");

            // Get XAML items from Grid
            Grid elements = (Grid)((Button)sender).Parent;

            // Retrieve each field from the edit menu
            try
            {
                DateTimeOffset date = (DateTimeOffset)((CalendarDatePicker)elements.FindName("CDP_edit_date")).Date;
                int hour = (int)((TimePicker)elements.FindName("TP_edit_time")).SelectedTime.Value.Hours;
                int minute = (int)((TimePicker)elements.FindName("TP_edit_time")).SelectedTime.Value.Minutes;
                int durHour = (int)((ComboBox)elements.FindName("TB_edit_durHours")).SelectedItem;
                int durMinute = (int)((ComboBox)elements.FindName("TB_edit_durMins")).SelectedItem;
                TaskType taskType = (TaskType)((ComboBox)elements.FindName("CB_edit_taskType")).SelectedItem;
                RepeatType repeatType = (RepeatType)((ComboBox)elements.FindName("CB_edit_repeat")).SelectedItem;
                string title = ((TextBox)elements.FindName("TB_edit_title")).Text;
                string description = ((TextBox)elements.FindName("TB_edit_description")).Text;
                YN required = (YN)((ComboBox)elements.FindName("CB_edit_required")).SelectedItem;
                string url = ((TextBox)elements.FindName("TB_edit_url")).Text;

                // Create a new task from the schedule
                SmartTask newTask = schedule.CreateTask(nextEventID++, date, hour, minute, durHour, durMinute, taskType, repeatType, title, description, required, url);

                // Remove the existing task
                ((SmartTask)((Button)sender).DataContext).DeleteTask();

                // Add the updated task back to the schedule
                schedule.AddTask(newTask);
            }
            catch(Exception err)
            {
                Debug.WriteLine($"Exception thrown in task update: {err.Message}");
            }

            RefreshScheduleView();
        }

        public void RefreshScheduleView()
        {
            // Refreshes the schedule viewer display
            LV_Schedule.ItemsSource = null;
            LV_Schedule.ItemsSource = schedule.GetTastsAt(selectedDate);
        }



    } // End of namespace


}
