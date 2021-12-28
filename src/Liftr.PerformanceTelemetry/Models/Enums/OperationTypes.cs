//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.Liftr.PerformanceTelemetry.Models.Enums
{
    /// <summary>
    /// Type of operation based on whether the method was called during the start of the API call or during the end of it
    /// </summary>
    public enum OperationTypes
    {
        Start,
        Stop,
    }
}
