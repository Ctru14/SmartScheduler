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
            selectedDate = DateTime.Now.Date;

            RefreshScheduleView();

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
            {
                TB_AsteriskTitle.Text = ""; 
            }

            // Date
            if (!CDP_NewItemDate.Date.HasValue)
            {
                filled = false;
                TB_AsteriskDate.Text = "*";
            } else
            {
                TB_AsteriskDate.Text = "";
            }

            // Task type
            if (CB_TypePicker.SelectedItem == null)
            {
                filled = false;
                TB_AsteriskType.Text = "*";
            }
            else
            {
                TB_AsteriskType.Text = "";
            }

            // Duration 
            if (CB_DurHoursPicker.SelectedItem == null &&
                CB_TypePicker.SelectedItem != null && (TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay) // duration is not required for a FullDay event
            {
                filled = false;
                TB_AsteriskDuration.Text = "*";
            }
            else
            {
                TB_AsteriskDuration.Text = "";
            }

            // Duration is not required for a FullDay event
            if (CB_DurMinsPicker.SelectedItem == null &&
                CB_TypePicker.SelectedItem != null && (TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay)
            {
                filled = false;
                TB_AsteriskDuration.Text = "*";
            }
            else
            {
                TB_AsteriskDuration.Text = "";
            }

            // Time
            if (!TP_NewItemTime.SelectedTime.HasValue &&
                CB_TypePicker.SelectedItem != null && (TaskType)CB_TypePicker.SelectedItem != TaskType.FullDay)
            {
                filled = false;
                TB_AsteriskTime.Text = "*";
            }
            else
            {
                TB_AsteriskTime.Text = "";
            }

            // Required?
            // *** Assume no selection means Yes required ***
            if (CB_RequiredPicker.SelectedValue == null)
            {
                CB_RequiredPicker.SelectedValue = YN.Yes;
            }

            // Repeat
            // *** Assume no selection means None repeat ***
            if (CB_RepeatPicker.SelectedValue == null)
            {
                CB_RepeatPicker.SelectedValue = RepeatType.None;
            }

            // Repeat #times
            // TODO - make repeat events show up in repeated days!
            int repeatNumTimes = 1;
            if (CB_RepeatPicker.SelectedValue != null && (RepeatType)CB_RepeatPicker.SelectedValue != RepeatType.None)
            {
                // Repeat is selected - we need to parse the number of times // TODO
                try
                {
                    repeatNumTimes = int.Parse(TB_NewRepeatTimes.Text);
                    if (repeatNumTimes < 1)
                    {
                        throw new Exception("Value of repeat times must be at least 1");
                    }
                    TB_AsteriskRepeatTimes.Text = "";
                }
                catch (FormatException fe)
                {
                    Debug.WriteLine($"Improper event repeat input: Exception = {fe}");
                    filled = false;
                    TB_AsteriskRepeatTimes.Text = "*";
                }
            }


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
            int hour = 0, minute = 0;
            if (TP_NewItemTime.SelectedTime != null)
            {
                hour = TP_NewItemTime.SelectedTime.Value.Hours;
                minute = TP_NewItemTime.SelectedTime.Value.Minutes;
            }
            DateTime when = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            // Get duration
            int durHour = 0, durMinute = 0;
            if (CB_DurHoursPicker.SelectedItem != null)
            {
                durHour = (int)CB_DurHoursPicker.SelectedItem;
                durMinute = (int)CB_DurMinsPicker.SelectedItem;
            }

            // Type classification
            TaskType taskType = (TaskType) CB_TypePicker.SelectedValue;
            RepeatType repeatType = CB_RepeatPicker.SelectedValue == null ? RepeatType.None : (RepeatType) CB_RepeatPicker.SelectedValue;
            YN required = CB_RequiredPicker.SelectedValue == null ? YN.Yes : (YN) CB_RequiredPicker.SelectedValue;

            // Create a new task from the schedule
            SmartTask newTask = schedule.CreateTask(nextEventID++, date, hour, minute, durHour, durMinute, taskType, repeatType, repeatNumTimes, TB_NewTitle.Text, TB_NewDescription.Text, required, TB_NewLocation.Text, TB_NewURL.Text);

            // Add to global schedule variable
            schedule.AddTask(newTask);

            if (selectedDate.Date == when.Date)
            {
                //Update the ListView viewer when adding a new task to the current date
                Debug.WriteLine($"Selected: {selectedDate.Date}, when: {when.Date}");
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
                SmartTask existingTask = (SmartTask)((Button)sender).DataContext;

                // Null fields were handled when creating the tasks, so these should have selections
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
                string location = ((TextBox)elements.FindName("TB_edit_location")).Text;
                string url = ((TextBox)elements.FindName("TB_edit_url")).Text;

                string repeatNumTimesText = ((TextBox)elements.FindName("TB_edit_repeatNumTimes")).Text;
                int repeatNumTimes;
                try
                {
                    repeatNumTimes = int.Parse(repeatNumTimesText);
                }
                catch
                {
                    repeatNumTimes = -1;
                }

                if (repeatNumTimes < 1)
                {
                    repeatNumTimes = existingTask.repeatNumTimes;
                }

                // Create a new task from the schedule
                SmartTask newTask = schedule.CreateTask(nextEventID++, date, hour, minute, durHour, durMinute, taskType, repeatType, repeatNumTimes, title, description, required, location, url);

                // Remove the existing task
                existingTask.DeleteTask();

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
            Debug.WriteLine($"RefreshScheduleView: date {selectedDate}");
            LV_Schedule.ItemsSource = null;
            LV_Schedule.ItemsSource = schedule.GetTastsAt(selectedDate.Date);
        }



    } // End of SmartScheduler namespace


}
