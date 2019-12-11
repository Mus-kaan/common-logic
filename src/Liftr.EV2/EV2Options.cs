//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2
{
    public class EV2Options
    {
        public string ServiceTreeName { get; set; }

        public Guid ServiceTreeId { get; set; }

        public string NotificationEmail { get; set; }

        public IEnumerable<TargetEnvironment> TargetEnvironments { get; set; }

        public void CheckValid()
        {
            if (TargetEnvironments == null || !TargetEnvironments.Any())
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(TargetEnvironments)} is not empty.");
                throw ex;
            }

            foreach (var targetEnvironment in TargetEnvironments)
            {
                if (targetEnvironment.Regions == null || !targetEnvironment.Regions.Any())
                {
                    var ex = new InvalidOperationException($"Please make sure {nameof(targetEnvironment.Regions)} is not empty for environment {targetEnvironment.EnvironmentName}.");
                    throw ex;
                }
            }
        }

        public EV2Options Clone()
        {
            return this.ToJson().FromJson<EV2Options>();
        }
    }

    public class TargetEnvironment
    {
        public EnvironmentType EnvironmentName { get; set; }

        public ReleaseRunner RunnerInformation { get; set; }

        public IEnumerable<string> Regions { get; set; }
    }

    public class ReleaseRunner
    {
        public string Location { get; set; }

        public Guid SubscriptionId { get; set; }

        public string UserAssignedManagedIdentityResourceId { get; set; }
    }
}
