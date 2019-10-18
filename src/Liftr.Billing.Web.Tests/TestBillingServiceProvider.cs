//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Moq;
using Serilog;

namespace Microsoft.Liftr.Billing.Web.Tests
{
    internal class TestBillingServiceProvider : Mock<IBillingServiceProvider>
    {
        public PushAgentUsageTable UsageTable = new PushAgentUsageTable();
        public PushAgentUsageQueue UsageQueue = new PushAgentUsageQueue();
        private static readonly Serilog.ILogger s_billingLogger = new LoggerConfiguration().CreateLogger();

        public TestBillingServiceProvider()
        {
            Setup(p => p.GetPushAgentUsageTable()).Returns(UsageTable.Object);
            Setup(p => p.GetPushAgentUsageQueue()).Returns(UsageQueue.Object);
            Setup(p => p.GetPushAgentClient()).Returns(new PushAgentClient(Object, s_billingLogger));
        }
    }
}