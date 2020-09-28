//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Monitoring.Whale.Models
{
    public class MonitoredResourceComparer : EqualityComparer<MonitoredResource>
    {
        public override bool Equals(MonitoredResource x, MonitoredResource y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }

            return x.Id.OrdinalEquals(y.Id);
        }

        public override int GetHashCode(MonitoredResource obj)
        {
            return obj?.Id.ToLowerInvariant().GetHashCode() ?? 0;
        }
    }
}
