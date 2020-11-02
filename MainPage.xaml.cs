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
        int changed = 0;
        public MainPage()
        {
            this.InitializeComponent();

            CB_TypePicker.ItemsSource = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToList();
            CB_RequiredPicker.ItemsSource = Enum.GetValues(typeof(YN)).Cast<YN>().ToList();
            CB_RepeatPicker.ItemsSource = Enum.GetValues(typeof(RepeatType)).Cast<RepeatType>().ToList();
            CB_DurHoursPicker.ItemsSource = SmartTask.hours;
            CB_DurMinsPicker.ItemsSource = SmartTask.mins;
        }

        private void CalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            //TB_Title.Text = "Changed text!" + changed++ + args.Item;
            //Console.WriteLine(args.Item);
        }

        private void CV_MainCalendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            TB_DaySchedule.Text = "Schedule: " + args.AddedDates.First().Date.ToString("d");
            CDP_NewItemDate.Date = args.AddedDates.First().Date;
        }
    }
}
