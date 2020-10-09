//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Liftr.ACIS.Relay
{
    public interface IACISOperation
    {
        Task LogErrorAsync(string message);

        Task LogInfoAsync(string message);

        Task LogVerboseAsync(string message);

        Task LogWarningAsync(string message);

        Task SuccessfulFinishAsync(string result);

        Task FailAsync(string result);
    }
}
