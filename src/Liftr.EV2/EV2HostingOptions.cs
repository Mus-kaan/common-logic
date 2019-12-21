//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2
{
    public class EV2HostingOptions : BaseEV2Options
    {
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
                targetEnvironment.CheckValid();
            }
        }

        public EV2HostingOptions Clone()
        {
            return this.ToJson().FromJson<EV2HostingOptions>();
        }
    }

    public class TargetEnvironment
    {
        public EnvironmentType EnvironmentName { get; set; }

        public EV2RunnerInfomation RunnerInformation { get; set; }

        public IEnumerable<string> Regions { get; set; }

        public void CheckValid()
        {
            if (Regions == null || !Regions.Any())
            {
                var ex = new InvalidOperationException($"Please make sure {nameof(Regions)} is not empty for environment {EnvironmentName}.");
                throw ex;
            }

            if (RunnerInformation == null)
            {
                var ex = new InvalidOperationException($"Please make sure '{nameof(RunnerInformation)}' is not empty for environment {EnvironmentName}.");
                throw ex;
            }

            RunnerInformation.CheckValid();
        }
    }
}
