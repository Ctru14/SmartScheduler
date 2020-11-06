using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SmartScheduler
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public SmartSchedule schedule = new SmartSchedule();

        private DateTime _selectedDate;
        public DateTime selectedDate {
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                TB_DaySchedule.Text = "Schedule: " + selectedDate.ToString("d");
            }
        }
        public MainPage()
        {
            this.InitializeComponent();

            CB_TypePicker.ItemsSource = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToList();
            CB_RequiredPicker.ItemsSource = Enum.GetValues(typeof(YN)).Cast<YN>().ToList();
            CB_RepeatPicker.ItemsSource = Enum.GetValues(typeof(RepeatType)).Cast<RepeatType>().ToList();
            CB_DurHoursPicker.ItemsSource = SmartTask.hours;
            CB_DurMinsPicker.ItemsSource = SmartTask.mins;

            selectedDate = DateTime.Now;

        }

        private void CV_MainCalendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            selectedDate = args.AddedDates.First().Date;
            CDP_NewItemDate.Date = args.AddedDates.First().Date;
        }

        private void PB_AddToSchedule_Click(object sender, RoutedEventArgs e)
        {
            bool filled = true;
            // Check to see if required fields are filled ijn
            if (TB_NewTitle.Text.Length == 0)
            {
                filled = false;
                // TODO: UPDATE THIS TO SHOW
            }
            if (!CDP_NewItemDate.Date.HasValue)
            {
                filled = false;
                // TODO: UPDATE THIS TO SHOW
            }
            if (!TP_NewItemTime.SelectedTime.HasValue)
            {
                filled = false;
                // TODO: UPDATE THIS TO SHOW
            }
            if (CB_RequiredPicker.SelectedValue == null)
            {
                filled = false;
                // TODO: UPDATE THIS TO SHOW
            }

            if (!filled) return;

            // Create SmartTask object out of the text fields
            SmartTask newTask = new SmartTask();

            // Get date
            DateTimeOffset date = CDP_NewItemDate.Date.Value;
            int hour = TP_NewItemTime.SelectedTime.Value.Hours;
            int minute = TP_NewItemTime.SelectedTime.Value.Minutes;
            DateTime when = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);


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

        }
    }
}
