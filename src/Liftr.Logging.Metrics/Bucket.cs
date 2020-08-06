//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Liftr.Logging.Metrics
{
    /// <summary>
    /// The content we send to statsd.
    /// Here's some examples https://msazure.visualstudio.com/One/_git/Compute-Runtime-Tux-GenevaContainers?path=%2Fdocker_geneva_samples%2FAKSGenevaSample%2FDotnetConsole%2FProgram.cs
    /// </summary>
    public class Bucket
    {
        public Bucket(string ns, string metric, Dictionary<string, string> dimension = null)
        {
            Namespace = ns;
            Metric = metric;
            Dims = dimension;
        }

        public string Namespace { get; set; }

        public string Metric { get; set; }

        public Dictionary<string, string> Dims { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}