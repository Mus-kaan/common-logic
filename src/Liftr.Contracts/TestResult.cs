//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public class TestResult
    {
        public string OperationName { get; set; }

        public string HelpText { get; set; }

        public string Component { get; set; } = "Unknown";

        public string TestClass { get; set; } = "Unknown";

        public string TestMethod { get; set; } = "Unknown";

        public bool IsFailure { get; set; }

        public long DurationMilliseconds { get; set; }

        public string TestCloudType { get; set; } = "Unknown";

        public string TestAzureRegion { get; set; } = "Unknown";

        public string TestRegionCategory { get; set; } = "Unknown";
    }
}
