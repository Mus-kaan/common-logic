//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    // ILiftrResourceProperties defines the common properties which should be included in all Liftr resources' ARM property bag
    public interface ILiftrResourceProperties
    {
        // ResourceCategory can be used by ARG query to retrieve all the Liftr resources across all providers.
        // If the RP doesn't need to be part of any predefined categories, set it to be Unknown
        public LiftrResourceCategory ResourceCategory { get; set; }

        // ResourcePreference can be used by ARG query to sort the result
        // By default set it to 0
        public uint ResourcePreference { get; set; }
    }
}
