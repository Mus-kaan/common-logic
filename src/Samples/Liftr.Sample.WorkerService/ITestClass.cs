//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Liftr.Sample.WorkerService
{
    public interface ITestClass
    {
        DateTime TestMethod();

        Task<DateTime> TestMethodAsync();

        Task<DateTime> TestMethodWithExceptionAsync();
    }
}
