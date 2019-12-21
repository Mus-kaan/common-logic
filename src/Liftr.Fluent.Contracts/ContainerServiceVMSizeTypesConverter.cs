//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Newtonsoft.Json;
using System;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public class ContainerServiceVMSizeTypesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ContainerServiceVMSizeTypes);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var value = (string)reader.Value;
            return ContainerServiceVMSizeTypes.Parse(value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (value != null)
            {
                ContainerServiceVMSizeTypes type = (ContainerServiceVMSizeTypes)value;

                writer.WriteValue(type.Value);
            }
        }
    }
}
