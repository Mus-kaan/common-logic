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
    [TraitDiscoverer(PublicCentralUSEUAPDiscoverer.DiscovererTypeName, TraitConstants.AssemblyName)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PublicCentralUSEUAPAttribute : Attribute, ITraitAttribute
    {
    }

    public class PublicCentralUSEUAPDiscoverer : ITraitDiscoverer
    {
        internal const string DiscovererTypeName = TraitConstants.Namespace + "." + nameof(PublicCentralUSEUAPDiscoverer);

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>(TraitConstants.Category, TraitConstants.CategoryVaule);
            yield return new KeyValuePair<string, string>(nameof(CloudType), CloudType.Public.ToString());
            yield return new KeyValuePair<string, string>(nameof(AzureRegion), AzureRegion.CentralUSEUAP.Name);
            yield return new KeyValuePair<string, string>(TraitConstants.RegionCategory, "PublicCentralUSEUAP");
        }
    }
}
