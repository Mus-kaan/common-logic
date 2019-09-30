//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.Logging
{
    public interface ITimedOperation : IDisposable
    {
        void SetProperty(string name, string value);

        void SetProperty(string name, int value);

        void SetProperty(string name, double value);

        void SetContextProperty(string name, string value);

        void SetContextProperty(string name, int value);

        void SetContextProperty(string name, double value);

        void SetResultDescription(string resultDescription);

        void FailOperation(string message = null);
    }
}
