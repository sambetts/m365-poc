namespace OfficeNotifications.Functions.Models
{

    public class TimerJobRefreshInfo
    {
        public TimerJobRefreshScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class TimerJobRefreshScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
