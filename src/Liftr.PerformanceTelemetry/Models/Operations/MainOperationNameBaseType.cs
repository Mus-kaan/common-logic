//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.PerformanceTelemetry.Models.Operations
{
    /// <summary>
    /// RPs will extend this abstract class to write their own main operations as part of their service only.
    /// </summary>
    public abstract class MainOperationNameBaseType
    {
        public readonly string Value;

        public MainOperationNameBaseType(string value)
        {
            Value = value;
        }
    }
}