//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.Utilities;

namespace Liftr.Sample.Web
{
    public class DatadogMonitorResource : ARMResource
    {
        /// <summary>
        /// ARM id of the monitor resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public new string Id
        {
            get => base.Id;

            set => base.Id = value;
        }

        /// <summary>
        /// Name of the monitor resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public new string Name
        {
            get => base.Name;

            set => base.Name = value;
        }

        /// <summary>
        /// The type of the monitor resource.
        /// </summary>
        [SwaggerExtension(MarkAsReadOnly = true)]
        public override string Type { get; set; } = "Microsoft.Datadog/monitors";
    }
}
