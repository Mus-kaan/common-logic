//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.Notifications
{
    // reference to the ARN document of this payload:
    // https://armwiki.azurewebsites.net/rpaas/resourcenotification.html#consume-azureresourcenotification-messages
    public class NotificationPayload
    {
        /// <summary>
        /// ID for the notification
        /// NOTE: this is NOT a resource ID of the observer nor observee
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("data")]
        public NotificationData Data { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("eventTime")]
        public string EventTime { get; set; }
    }

    public class NotificationData
    {
        [JsonProperty("resources")]
        public List<NotificationResources> Resources { get; set; }

        [JsonProperty("resourceLocation")]
        public string ResourceLocation { get; set; }

        [JsonProperty("homeTenantId")]
        public string HomeTenantId { get; set; }
    }

    public class NotificationResources
    {
        [JsonProperty("$id")]
        public string Id { get; set; }

        [JsonProperty("resourceId")]
        public string ResourceId { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("armResource")]
        public ArmResourceNotificationData ArmResource { get; set; }
    }

    public class ArmResourceNotificationData
    {
        [JsonProperty("properties")]
        public ArmResourceNotificationProperties Properties { get; set; }
    }

    public class ArmResourceNotificationProperties
    {
        [JsonProperty("marketplacePartnerId")]
        public string MarketplacePartnerId { get; set; }
    }
}
