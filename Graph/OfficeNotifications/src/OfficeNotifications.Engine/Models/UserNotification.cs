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
