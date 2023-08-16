namespace Web.Config
{
    public class WebAppConfig : PropertyBoundConfig
    {
        public WebAppConfig(IConfiguration config) : base(config)
        {
        }


        [ConfigValue] public string SendGridApiKey { get; set; } = string.Empty;
        [ConfigValue] public string CaptchaSecret { get; set; } = string.Empty;
        [ConfigValue] public string EmailsTo { get; set; } = string.Empty;
        [ConfigValue] public string EmailsFrom { get; set; } = string.Empty;

        [ConfigSection("ConnectionStrings")] public ConnectionStrings ConnectionStrings { get; set; } = null!;

    }


    public class ConnectionStrings : PropertyBoundConfig
    {
        public ConnectionStrings(IConfigurationSection config) : base(config)
        {
        }

        [ConfigValue]
        public string Storage { get; set; } = string.Empty;


    }
}
