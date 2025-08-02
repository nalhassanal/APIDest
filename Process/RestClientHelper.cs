using System;

namespace Process
{
    public static class RestClientHelper
    {
        public static TimeSpan GetRestClientTimeOutTimeSpan(string minutesString = "")
        {
            if (string.IsNullOrEmpty(minutesString))
            {
                return TimeSpan.FromMinutes(30);
            }
            else if (double.TryParse(minutesString, out double _mins))
            {

                return TimeSpan.FromMinutes(_mins);
            }
            else
            {
                return TimeSpan.FromMinutes(30);
            }
        }
    }
}
