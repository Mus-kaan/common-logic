//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// Enumeration of the Azure datacenter regions. See https://azure.microsoft.com/regions/
    /// This is based on the fluent SDK: https://github.com/Azure/azure-libraries-for-net/blob/master/src/ResourceManagement/ResourceManager/Region.cs
    /// To remove the fluent SDK dependency, this code is duplicated and maintained another version to ease add new AzureRegion.
    /// </summary>
    public class AzureRegion
    {
        public AzureRegion(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var standardName = name.Replace(" ", string.Empty).ToLowerInvariant();
            var matchedRegion = s_knownRegions.FirstOrDefault(r => r.Name.OrdinalEquals(standardName));
            if (matchedRegion == null)
            {
                throw new ArgumentOutOfRangeException($"The region name '{name}' is not a known region");
            }

            Name = matchedRegion.Name;
            DisplayName = matchedRegion.DisplayName;
            ShortName = matchedRegion.ShortName;
        }

        private AzureRegion(string name, string displayName, string shortName)
        {
            Name = name;
            DisplayName = displayName;
            ShortName = shortName;
        }

        /// <summary>
        /// Name of the region. This is in lower case without space, e.g. westus2
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Display Name of the region. This is the one showing in Azure portal, e.g. West US 2
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Short Name of the region. e.g. wus2 is for 'West US 2'
        /// </summary>
        public string ShortName { get; }

        public static bool operator ==(AzureRegion lhs, AzureRegion rhs)
        {
            if (object.ReferenceEquals(lhs, null))
            {
                return object.ReferenceEquals(rhs, null);
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(AzureRegion lhs, AzureRegion rhs)
        {
            return !(lhs == rhs);
        }

        public static AzureRegion FromShortName(string shortName)
        {
            var matchedRegion = s_knownRegions.FirstOrDefault(r => r.ShortName.OrdinalEquals(shortName));
            if (matchedRegion == null)
            {
                throw new ArgumentOutOfRangeException($"The region short name '{shortName}' is not a known region");
            }

            return matchedRegion;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AzureRegion))
            {
                return false;
            }

            if (object.ReferenceEquals(obj, this))
            {
                return true;
            }

            AzureRegion rhs = (AzureRegion)obj;
            if (Name == null)
            {
                return rhs.Name == null;
            }

            return Name.Equals(rhs.Name, System.StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Name;
        }

        #region Americas
        public static readonly AzureRegion USWest = new AzureRegion("westus", "West US", "wus");
        public static readonly AzureRegion USWest2 = new AzureRegion("westus2", "West US 2", "wus2");
        public static readonly AzureRegion USWest3 = new AzureRegion("westus3", "West US 3", "wus3");
        public static readonly AzureRegion USCentral = new AzureRegion("centralus", "Central US", "cus");
        public static readonly AzureRegion USEast = new AzureRegion("eastus", "East US", "eus");
        public static readonly AzureRegion USEast2 = new AzureRegion("eastus2", "East US 2", "eus2");
        public static readonly AzureRegion USNorthCentral = new AzureRegion("northcentralus", "North Central US", "ncus");
        public static readonly AzureRegion USSouthCentral = new AzureRegion("southcentralus", "South Central US", "scus");
        public static readonly AzureRegion USWestCentral = new AzureRegion("westcentralus", "West Central US", "wcus");
        public static readonly AzureRegion CanadaCentral = new AzureRegion("canadacentral", "Canada Central", "cca");
        public static readonly AzureRegion CanadaEast = new AzureRegion("canadaeast", "Canada East", "eca");
        public static readonly AzureRegion BrazilSouth = new AzureRegion("brazilsouth", "Brazil South", "sbr");
        #endregion

        #region EUAP regions
        public static readonly AzureRegion EastUS2EUAP = new AzureRegion("eastus2euap", "East US 2 EUAP", "eus2e");
        public static readonly AzureRegion CentralUSEUAP = new AzureRegion("centraluseuap", "Central US EUAP", "cuse");
        #endregion

        #region Europe
        public static readonly AzureRegion EuropeNorth = new AzureRegion("northeurope", "North Europe", "neu");
        public static readonly AzureRegion EuropeWest = new AzureRegion("westeurope", "West Europe", "weu");
        public static readonly AzureRegion UKSouth = new AzureRegion("uksouth", "UK South", "suk");
        public static readonly AzureRegion UKWest = new AzureRegion("ukwest", "UK West", "wuk");
        public static readonly AzureRegion FranceCentral = new AzureRegion("francecentral", "France Central", "cfr");
        public static readonly AzureRegion FranceSouth = new AzureRegion("francesouth", "France South", "sfr");
        public static readonly AzureRegion SwitzerlandNorth = new AzureRegion("switzerlandnorth", "Switzerland North", "nch");
        public static readonly AzureRegion SwitzerlandWest = new AzureRegion("switzerlandwest", "Switzerland West", "wch");
        public static readonly AzureRegion GermanyNorth = new AzureRegion("germanynorth", "GermanyNorth", "nde");
        public static readonly AzureRegion GermanyWestCentral = new AzureRegion("germanywestcentral", "Germany West Central", "wcde");
        public static readonly AzureRegion NorwayWest = new AzureRegion("norwaywest", "Norway West", "wno");
        public static readonly AzureRegion NorwayEast = new AzureRegion("norwayeast", "Norway East", "eno");
        #endregion

        #region Asia
        public static readonly AzureRegion AsiaEast = new AzureRegion("eastasia", "East Asia", "hk");
        public static readonly AzureRegion AsiaSouthEast = new AzureRegion("southeastasia", "Southeast Asia", "sing");
        public static readonly AzureRegion JapanEast = new AzureRegion("japaneast", "Japan East", "tyo");
        public static readonly AzureRegion JapanWest = new AzureRegion("japanwest", "JapanWest", "wjp");
        public static readonly AzureRegion AustraliaEast = new AzureRegion("australiaeast", "Australia East", "eau");
        public static readonly AzureRegion AustraliaSouthEast = new AzureRegion("australiasoutheast", "Australia Southeast", "seau");
        public static readonly AzureRegion AustraliaCentral = new AzureRegion("australiacentral", "Australia Central", "cau");
        public static readonly AzureRegion AustraliaCentral2 = new AzureRegion("australiacentral2", "Australia Central 2", "cau2");
        public static readonly AzureRegion IndiaCentral = new AzureRegion("centralindia", "Central India", "cin");
        public static readonly AzureRegion IndiaSouth = new AzureRegion("southindia", "South India", "sin");
        public static readonly AzureRegion IndiaWest = new AzureRegion("westindia", "West India", "win");
        public static readonly AzureRegion KoreaSouth = new AzureRegion("koreasouth", "Korea South", "skr");
        public static readonly AzureRegion KoreaCentral = new AzureRegion("koreacentral", "Korea Central", "sel");
        #endregion

        #region Middle East and Africa
        public static readonly AzureRegion UAECentral = new AzureRegion("uaecentral", "UAE Central", "cuae");
        public static readonly AzureRegion UAENorth = new AzureRegion("uaenorth", "UAE North", "nuae");
        public static readonly AzureRegion SouthAfricaNorth = new AzureRegion("southafricanorth", "South Africa North", "nza");
        public static readonly AzureRegion SouthAfricaWest = new AzureRegion("southafricawest", "South Africa West", "wza");
        #endregion

        #region China
        public static readonly AzureRegion ChinaNorth = new AzureRegion("chinanorth", "China North", "bj");
        public static readonly AzureRegion ChinaEast = new AzureRegion("chinaeast", "China East", "sha");
        public static readonly AzureRegion ChinaNorth2 = new AzureRegion("chinanorth2", "China North 2", "bj2");
        public static readonly AzureRegion ChinaEast2 = new AzureRegion("chinaeast2", "China East 2", "sha2");
        #endregion

        #region German
        public static readonly AzureRegion GermanyCentral = new AzureRegion("germanycentral", "Germany Central", "cde");
        public static readonly AzureRegion GermanyNorthEast = new AzureRegion("germanynortheast", "Germany North East", "nede");
        #endregion

        private static readonly List<AzureRegion> s_knownRegions = new List<AzureRegion>()
        {
            AzureRegion.USWest,
            AzureRegion.USWest2,
            AzureRegion.USWest3,
            AzureRegion.USCentral,
            AzureRegion.USEast,
            AzureRegion.USEast2,
            AzureRegion.USNorthCentral,
            AzureRegion.USSouthCentral,
            AzureRegion.USWestCentral,
            AzureRegion.CanadaCentral,
            AzureRegion.CanadaEast,
            AzureRegion.BrazilSouth,
            AzureRegion.EastUS2EUAP,
            AzureRegion.CentralUSEUAP,
            AzureRegion.EuropeNorth,
            AzureRegion.EuropeWest,
            AzureRegion.UKSouth,
            AzureRegion.UKWest,
            AzureRegion.FranceCentral,
            AzureRegion.FranceSouth,
            AzureRegion.SwitzerlandNorth,
            AzureRegion.SwitzerlandWest,
            AzureRegion.GermanyNorth,
            AzureRegion.GermanyWestCentral,
            AzureRegion.NorwayWest,
            AzureRegion.NorwayEast,
            AzureRegion.AsiaEast,
            AzureRegion.AsiaSouthEast,
            AzureRegion.JapanEast,
            AzureRegion.JapanWest,
            AzureRegion.AustraliaEast,
            AzureRegion.AustraliaSouthEast,
            AzureRegion.AustraliaCentral,
            AzureRegion.AustraliaCentral2,
            AzureRegion.IndiaCentral,
            AzureRegion.IndiaSouth,
            AzureRegion.IndiaWest,
            AzureRegion.KoreaSouth,
            AzureRegion.KoreaCentral,
            AzureRegion.UAECentral,
            AzureRegion.UAENorth,
            AzureRegion.SouthAfricaNorth,
            AzureRegion.SouthAfricaWest,
            AzureRegion.ChinaNorth,
            AzureRegion.ChinaEast,
            AzureRegion.ChinaNorth2,
            AzureRegion.ChinaEast2,
            AzureRegion.GermanyCentral,
            AzureRegion.GermanyNorthEast,
        };
    }
}
