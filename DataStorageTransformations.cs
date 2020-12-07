using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SmartScheduler
{
    public static class DataStorageTransformations
    {
        // SolidColorBrush
        public static string SolidColorBrush_ToStorageString(SolidColorBrush colorBrush)
        {
            int c = 0;
            c += colorBrush.Color.R << 16;
            c += colorBrush.Color.G << 8;
            c += colorBrush.Color.B;
            return c.ToString();
        }

        public static SolidColorBrush SolidColorBrush_FromStorageString(string s)
        {
            int c = Int32.Parse(s);
            byte r, g, b;
            r = (byte)(c >> 16);
            g = (byte)((c >> 8) & 0xFF);
            b = (byte)(c & 0xFF);
            return new SolidColorBrush(Color.FromArgb(0xFF, r, g, b));
        }

        // DateTime
        public static string DateTime_ToStorageString(DateTime dt)
        {
            return dt.ToString("u");
        }

        public static DateTime DateTime_FromStorageString(string s)
        {
            DateTime dt;
            if (DateTime.TryParse(s, out dt)) return dt;
            else return new DateTime();
        }
    }
}
