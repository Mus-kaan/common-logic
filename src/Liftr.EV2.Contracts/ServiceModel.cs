//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of the Cloud Service Model of an Azure Service.
    /// </summary>
    public class ServiceModel : Document
    {
        public ServiceModel()
        {
            Schema = new Uri("http://schema.express.azure.com/schemas/2015-01-01-alpha/ServiceModel.json");
        }

        /// <summary>
        ///     Information about the service that can be used to uniquely identify it.
        /// </summary>
        public ServiceMetadata ServiceMetadata { get; set; }

        /// <summary>
        ///     The enumeration of the various resource group definitions that represent how to
        ///     construct the resource groups that constitute this cloud service.
        /// </summary>
        public IEnumerable<ServiceResourceGroupDefinition> ServiceResourceGroupDefinitions { get; set; }

        /// <summary>
        ///     The various resource groups that constitute this service.
        /// </summary>
        public IEnumerable<ServiceResourceGroup> ServiceResourceGroups { get; set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisModel"/> is reference-equal or value-equal to
        /// <paramref name="otherModel"/>.
        /// </summary>
        /// <param name="thisModel">An instance of <see cref="ServiceModel"/>. </param>
        /// <param name="otherModel">Another instance of <see cref="ServiceModel"/>. </param>
        public static bool operator ==(ServiceModel thisModel, ServiceModel otherModel)
        {
            return Equals(thisModel, otherModel);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisModel"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherModel"/>.
        /// </summary>
        /// <param name="thisModel">An instance of <see cref="ServiceModel"/>. </param>
        /// <param name="otherModel">Another instance of <see cref="ServiceModel"/>. </param>
        public static bool operator !=(ServiceModel thisModel, ServiceModel otherModel)
        {
            return !Equals(thisModel, otherModel);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisModel"/> is reference-equal or value-equal to
        /// <paramref name="otherModel"/>.
        /// </summary>
        /// <param name="thisModel">An instance of <see cref="ServiceModel"/>. </param>
        /// <param name="otherModel">Another instance of <see cref="ServiceModel"/>. </param>
        public static bool Equals(ServiceModel thisModel, ServiceModel otherModel)
        {
            if (Document.ReferenceEquals(thisModel, otherModel))
            {
                return true;
            }

            if (Document.ReferenceEquals(thisModel, null) || Document.ReferenceEquals(otherModel, null))
            {
                return false;
            }

            return thisModel.ServiceMetadata == otherModel.ServiceMetadata &&
                   thisModel.ServiceResourceGroupDefinitions.IsEquivalentTo(otherModel.ServiceResourceGroupDefinitions) &&
                   thisModel.ServiceResourceGroups.IsEquivalentTo(otherModel.ServiceResourceGroups);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherModel"/>.
        /// </summary>
        /// <param name="otherModel">Another instance of <see cref="ServiceModel"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherModel' is more relevant than 'obj'. ")]
        public override bool Equals(object otherModel)
        {
            return Equals(this, otherModel as ServiceModel);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            var definitionsHashCode = ServiceResourceGroupDefinitions.Sum(definition => definition.GetHashCode());
            var resourceGroupsHashCode = ServiceResourceGroups.Sum(resourceGroup => resourceGroup.GetHashCode());

            return ServiceMetadata.ServiceGroup.GetHashCode() +
                   ServiceMetadata.Environment.GetHashCode() +
                   definitionsHashCode +
                   resourceGroupsHashCode;
        }
    }
}