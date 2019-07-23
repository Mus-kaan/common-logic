//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Microsoft.Liftr
{
    public static class JsonExtensions
    {
        public static JsonSerializerSettings DefaultFormatterSettings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false },
            },
            Converters = new List<JsonConverter>
                {
                    new TimeSpanConverter(),
                    new StringEnumConverter(),
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
                },
        };

        public static JsonSerializer JsonSerializer => JsonSerializer.Create(DefaultFormatterSettings);

        public static string ToJsonString(this object self)
        {
            return ToJson(self, DefaultFormatterSettings);
        }

        public static string ToJson(this object self)
        {
            return ToJson(self, DefaultFormatterSettings);
        }

        public static JObject ToJObject(this object self)
        {
            return JObject.Parse(self.ToJson());
        }

        public static string ToJson(this object self, JsonSerializerSettings settings)
        {
            if (self == null)
            {
                return null;
            }

            return JsonConvert.SerializeObject(self, settings);
        }

        public static T FromJson<T>(this string json)
        {
            return FromJson<T>(json, DefaultFormatterSettings);
        }

        public static T FromJson<T>(this string json, JsonSerializerSettings settings)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static T FromJToken<T>(this JToken token)
        {
            return token == null ? default(T) : token.ToObject<T>(JsonSerializer);
        }

        public static DateTimeOffset ToDateTimeOffset(this JToken token)
        {
            // The double conversion ensures the original UTC time is not converted to local time
            return token == null ? default(DateTimeOffset) : token.ToJson().FromJson<DateTimeOffset>();
        }

        public static JToken Get(this JToken json, string propName)
        {
            var obj = json as JObject;
            if (obj == null)
            {
                throw new InvalidOperationException("Can only access child values on JObject");
            }

            return obj.GetValue(propName, StringComparison.OrdinalIgnoreCase);
        }

        public static JToken Get(this JToken json, params string[] propNames)
        {
            if (propNames == null)
            {
                throw new ArgumentNullException(nameof(propNames));
            }

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return propNames.Aggregate(json, (next, propName) => next.Get(propName));
        }

        public static T ValueOrDefault<T>(this JToken json)
        {
            return json == null ?
                default(T) :
                json.Value<T>();
        }

        internal class TimeSpanConverter : JsonConverter
        {
            /// <summary>
            /// Writes the <c>JSON</c>.
            /// </summary>
            /// <param name="writer">The writer.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, XmlConvert.ToString((TimeSpan)value));
            }

            /// <summary>
            /// Reads the <c>JSON</c>.
            /// </summary>
            /// <param name="reader">The reader.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value.</param>
            /// <param name="serializer">The serializer.</param>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return reader.TokenType != JsonToken.Null ? (object)XmlConvert.ToTimeSpan(serializer.Deserialize<string>(reader)) : null;
            }

            /// <summary>
            /// Determines whether this instance can convert the specified object type.
            /// </summary>
            /// <param name="objectType">Type of the object.</param>
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
            }
        }
    }
}
