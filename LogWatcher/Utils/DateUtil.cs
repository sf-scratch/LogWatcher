using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogWatcher.Utils
{
    public class DateUtil
    {
        public static string Now
        {
            get
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        public static string GetTimeStampMsg(string msg)
        {
            return $"{Now}：msg";
        }
    }
}
