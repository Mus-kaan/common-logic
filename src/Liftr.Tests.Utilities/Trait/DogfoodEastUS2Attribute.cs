//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Liftr.Tests.Utilities.Trait
{
    [TraitDiscoverer(DogfoodEastUS2Discoverer.DiscovererTypeName, TraitConstants.AssemblyName)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DogfoodEastUS2Attribute : Attribute, ITraitAttribute
    {
    }

    public class DogfoodEastUS2Discoverer : ITraitDiscoverer
    {
        internal const string DiscovererTypeName = TraitConstants.Namespace + "." + nameof(DogfoodEastUS2Discoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>(TraitConstants.Category, TraitConstants.CategoryVaule);
            yield return new KeyValuePair<string, string>(nameof(CloudType), CloudType.DogFood.ToString());
            yield return new KeyValuePair<string, string>(nameof(AzureRegion), AzureRegion.USEast2.Name);
            yield return new KeyValuePair<string, string>(TraitConstants.RegionCategory, "DogfoodEastUS2");
        }
    }
}
