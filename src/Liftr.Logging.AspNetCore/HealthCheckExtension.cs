//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Logging.AspNetCore
{
    public static class HealthCheckExtension
    {
        /// <summary>
        /// This callback should return the health check status. If it returns false, it will return 503 for 'GET /api/liveness-probe'.
        /// If 'GET /api/liveness-probe' is configured for k8s liveness probe, the container will be restarted when it is failing.
        /// By default, our Traffic Manager is also ping this path. So TM may mark this endpoint as disgraded when it is failing.
        /// It is highly recommended to make this call fast, it will timeout otherwise.
        /// It is also recommended to use another background task to do the real health check work and this callback only returns a preconfigured value.
        /// </summary>
        public static Func<bool> GetHealthCheckStatus { get; set; }
    }
}
