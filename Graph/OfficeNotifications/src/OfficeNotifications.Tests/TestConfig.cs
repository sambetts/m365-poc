using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using OfficeNotifications.Engine;
using OfficeNotifications.Engine.Models;
using System;

namespace OfficeNotifications.Tests
{
    public class TestConfig : Config
    {
        public TestConfig(IConfiguration config) : base(config)
        {
        }

        [ConfigValue]
        public string TestEmailAddress { get; set; } = string.Empty;


        [ConfigValue]
        public string TestUserId { get; set; } = string.Empty;


        [ConfigValue]
        public string TestX509Certificate2 { get; set; } = string.Empty;
        [ConfigSection("TestMessageNotificationEncryptedPayload")] public TestMessageNotificationEncryptedPayload TestMessageNotification { get; set; } = null!;
    }


    public class TestMessageNotificationEncryptedPayload : PropertyBoundConfig
    {
        public TestMessageNotificationEncryptedPayload(IConfigurationSection config) : base(config)
        {
        }

        [ConfigValue]
        public string Data { get; set; } = string.Empty;
        [ConfigValue]
        public string DataSignature { get; set; } = string.Empty;
        [ConfigValue]
        public string DataKey { get; set; } = string.Empty;
        [ConfigValue]
        public string EncryptionCertificateId { get; set; } = string.Empty;

        public string EncryptionCertificateThumbprint { get; set; } = string.Empty;

        internal EncryptedGraphResourceDataContent ToEncryptedContent()
        {
            return new EncryptedGraphResourceDataContent 
            {
                Data = Data, 
                DataKey = DataKey, 
                EncryptionCertificateId = EncryptionCertificateId,
                EncryptionCertificateThumbprint = EncryptionCertificateThumbprint,
                DataSignature = DataSignature
            };
        }
    }
}
