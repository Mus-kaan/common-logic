//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.Marketplace;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Marketplace
{
    public class MarketplaceSubscriptionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Guid);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var id = Guid.Parse((string)reader.Value);
            MarketplaceSubscription subscription = new MarketplaceSubscription(id);
            return subscription;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            MarketplaceSubscription subscription = (MarketplaceSubscription)value;
            writer.WriteValue(subscription.Id);
        }
    }
}
