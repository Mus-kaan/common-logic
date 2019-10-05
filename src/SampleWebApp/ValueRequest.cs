//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Hosting.Swagger;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWebApp
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnumType1
    {
        None,
        Val1,
        Val2,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnumType2
    {
        None,
        Val3,
        Val4,
    }

    public class ValueRequest
    {
        public string ItemName { get; set; }

        public EnumType1 EValue1 { get; set; }

        [SwaggerExtension(ExcludeFromSwagger = true)]
        public InternalClass InternalClass { get; set; }

        [SwaggerExtension(ExcludeFromSwagger = true)]
        public int InternalProperty { get; set; }

        [SwaggerExtension(MarkAsReadOnly = true)]
        public int ReadOnlyProperty { get; set; }
    }

    [SwaggerExtension(ExcludeFromSwagger = true)]
    public class InternalClass
    {
    }
}
