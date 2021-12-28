//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.PerformanceTelemetry.Models.Operations
{
    /// <summary>
    /// List of all the main operations that are common to multiple RPs and are not RP specific.
    /// </summary>
    public class MainCommonOperations : MainOperationNameBaseType
    {
        public MainCommonOperations(string value) : base(value)
        {
        }
    }
}