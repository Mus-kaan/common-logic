//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Logging.Metrics
{
    public interface IMetricSender
    {
        void Gauge(string metric, int value, Dictionary<string, string> dimension = null);

        void Gauge(string mdmNamespace, string metric, int value, Dictionary<string, string> dimension = null);
    }
}
