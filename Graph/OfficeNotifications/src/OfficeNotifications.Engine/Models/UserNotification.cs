using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficeNotifications.Engine.Models
{
    public class UserNotification
    {
        public UserNotification()
        {
            Received = DateTime.Now;
        }

        public string Message { get; set; } = string.Empty;
        public DateTime Received { get; set; } = DateTime.MinValue;
    }
}
