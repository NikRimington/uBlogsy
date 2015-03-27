using System;
using System.Globalization;

namespace uBlogsy.Common.Helpers
{
    public class DateHelper
    {
        /// <summary>
        /// Gets month name
        /// </summary>
        /// <param name="month"></param>
        /// <param name="abbreviate"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static string GetMonthName(int month, bool abbreviate, IFormatProvider provider)
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(provider);
            if (abbreviate) 
                return info.GetAbbreviatedMonthName(month);
            
            return info.GetMonthName(month);
        }


        /// <summary>
        /// Gets abbreviated month name
        /// </summary>
        /// <param name="month"></param>
        /// <param name="abbreviate"></param>
        /// <returns></returns>
        public static string GetMonthName(int month, bool abbreviate)
        {
            return GetMonthName(month, abbreviate, null);
        }


        /// <summary>
        /// Gets non-abbreviated month name.
        /// </summary>
        /// <param name="month"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static string GetMonthName(int month, IFormatProvider provider)
        {
            return GetMonthName(month, false, provider);
        }


        /// <summary>
        /// Gets non-abbreviated month name.
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public static string GetMonthName(int month)
        {
            return GetMonthName(month, false, null);
        }






        /// <summary>
        /// Gets day name.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="abbreviate"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static string GetDayName(int day, bool abbreviate, IFormatProvider provider)
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(provider);
            DateTime date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, day);

            if (abbreviate)
                return info.GetAbbreviatedDayName(date.DayOfWeek);

            return info.GetDayName(date.DayOfWeek);
        }


        /// <summary>
        /// Gets abbreviated day name.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="abbreviate"></param>
        /// <returns></returns>
        public static string GetDayName(int day, bool abbreviate)
        {
            return GetDayName(day, abbreviate, null);
        }


        /// <summary>
        /// Gets day name.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static string GetDayName(int day, IFormatProvider provider)
        {
            return GetDayName(day, false, provider);
        }


        /// <summary>
        /// Gets non-abbreviated day name.
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static string GetDayName(int day)
        {
            return GetDayName(day, false, null);
        }







        /// <summary>
        /// Determines which format the month name should be in based on format.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="monthNumber"></param>
        /// <returns></returns>
        public static string GetMonthNameWithFormat(int monthNumber, string format)
        {
            if (format == "M")
            {
                return monthNumber.ToString();
            }
            var date = new DateTime(2012, monthNumber, 1);

            var month = date.ToString(format);
            return month;
        }





        /// <summary>
        /// Determines which format the day name should be in based on format.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="dayNumber"></param>
        /// <returns></returns>
        public static string GetDayNameWithFormat(int dayNumber, string format)
        {
            if (format == "d")
            {
                return dayNumber.ToString();
            }

            var date = new DateTime(2012, 1, dayNumber);

            var day = date.ToString(format);
            return day;
        }


    }
}
