//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.DBService.Contracts.Tests
{
    public class ProvisioningStateTests
    {
        [Theory]
        [InlineData(ProvisioningState.Accepted, false)]
        [InlineData(ProvisioningState.Creating, false)]
        [InlineData(ProvisioningState.Updating, false)]
        [InlineData(ProvisioningState.Deleting, false)]
        [InlineData(ProvisioningState.Succeeded, true)]
        [InlineData(ProvisioningState.Failed, true)]
        [InlineData(ProvisioningState.Canceled, true)]
        [InlineData(ProvisioningState.Deleted, true)]
        public void CanCheckFinalState(ProvisioningState state, bool isFinalState)
        {
            Assert.Equal(isFinalState, state.IsFinalState());
        }
    }
}