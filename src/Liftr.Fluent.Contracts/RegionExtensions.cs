//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;

namespace Microsoft.Liftr.Fluent.Contracts
{
    public static class RegionExtensions
    {
        public static string ShortName(this Region region)
        {
            // https://github.com/Azure/azure-libraries-for-net/blob/master/src/ResourceManagement/ResourceManager/Region.cs
            if (region == Region.USWest)
            {
                return "wus";
            }
            else if (region == Region.USWest2)
            {
                return "wus2";
            }
            else if (region == Region.USCentral)
            {
                return "cus";
            }
            else if (region == Region.USEast)
            {
                return "eus";
            }
            else if (region == Region.USEast2)
            {
                return "eus2";
            }
            else if (region == Region.USNorthCentral)
            {
                return "ncus";
            }
            else if (region == Region.USSouthCentral)
            {
                return "scus";
            }
            else if (region == Region.USWestCentral)
            {
                return "wcus";
            }
            else if (region == Region.CanadaCentral)
            {
                return "cca";
            }
            else if (region == Region.CanadaEast)
            {
                return "eca";
            }
            else if (region == Region.BrazilSouth)
            {
                return "sbr";
            }
            else if (region == Region.EuropeNorth)
            {
                return "neu";
            }
            else if (region == Region.EuropeWest)
            {
                return "weu";
            }
            else if (region == Region.UKSouth)
            {
                return "suk";
            }
            else if (region == Region.UKWest)
            {
                return "wuk";
            }
            else if (region == Region.FranceCentral)
            {
                return "cfr";
            }
            else if (region == Region.FranceSouth)
            {
                return "sfr";
            }
            else if (region == Region.SwitzerlandNorth)
            {
                return "nch";
            }
            else if (region == Region.SwitzerlandWest)
            {
                return "wch";
            }
            else if (region == Region.GermanyNorth)
            {
                return "nde";
            }
            else if (region == Region.GermanyWestCentral)
            {
                return "wcde";
            }
            else if (region == Region.NorwayWest)
            {
                return "wno";
            }
            else if (region == Region.NorwayEast)
            {
                return "eno";
            }
            else if (region == Region.AsiaEast)
            {
                return "hk";
            }
            else if (region == Region.AsiaSouthEast)
            {
                return "sing";
            }
            else if (region == Region.JapanEast)
            {
                return "tyo";
            }
            else if (region == Region.JapanWest)
            {
                return "wjp";
            }
            else if (region == Region.AustraliaEast)
            {
                return "eau";
            }
            else if (region == Region.AustraliaSouthEast)
            {
                return "seau";
            }
            else if (region == Region.AustraliaCentral)
            {
                return "cau";
            }
            else if (region == Region.AustraliaCentral2)
            {
                return "cau2";
            }
            else if (region == Region.IndiaCentral)
            {
                return "cin";
            }
            else if (region == Region.IndiaSouth)
            {
                return "sin";
            }
            else if (region == Region.IndiaWest)
            {
                return "win";
            }
            else if (region == Region.KoreaSouth)
            {
                return "skr";
            }
            else if (region == Region.KoreaCentral)
            {
                return "sel";
            }
            else if (region == Region.UAECentral)
            {
                return "cuae";
            }
            else if (region == Region.UAENorth)
            {
                return "nuae";
            }
            else if (region == Region.SouthAfricaNorth)
            {
                return "nza";
            }
            else if (region == Region.SouthAfricaWest)
            {
                return "wza";
            }
            else if (region == Region.ChinaNorth)
            {
                return "bj";
            }
            else if (region == Region.ChinaEast)
            {
                return "sha";
            }
            else if (region == Region.ChinaNorth2)
            {
                return "bj2";
            }
            else if (region == Region.ChinaEast2)
            {
                return "sha2";
            }
            else if (region == Region.GermanyCentral)
            {
                return "cde";
            }
            else if (region == Region.GermanyNorthEast)
            {
                return "nede";
            }

            throw new ArgumentOutOfRangeException($"Azure region {region} does not have predefined short name.");
        }

        public static Region ParseShortAzureRegion(this string location)
        {
            if (location.OrdinalEquals("wus"))
            {
                return Region.USWest;
            }
            else if (location.OrdinalEquals("wus2"))
            {
                return Region.USWest2;
            }
            else if (location.OrdinalEquals("cus"))
            {
                return Region.USCentral;
            }
            else if (location.OrdinalEquals("eus"))
            {
                return Region.USEast;
            }
            else if (location.OrdinalEquals("eus2"))
            {
                return Region.USEast2;
            }
            else if (location.OrdinalEquals("ncus"))
            {
                return Region.USNorthCentral;
            }
            else if (location.OrdinalEquals("scus"))
            {
                return Region.USSouthCentral;
            }
            else if (location.OrdinalEquals("wcus"))
            {
                return Region.USWestCentral;
            }
            else if (location.OrdinalEquals("cca"))
            {
                return Region.CanadaCentral;
            }
            else if (location.OrdinalEquals("eca"))
            {
                return Region.CanadaEast;
            }
            else if (location.OrdinalEquals("sbr"))
            {
                return Region.BrazilSouth;
            }
            else if (location.OrdinalEquals("neu"))
            {
                return Region.EuropeNorth;
            }
            else if (location.OrdinalEquals("weu"))
            {
                return Region.EuropeWest;
            }
            else if (location.OrdinalEquals("suk"))
            {
                return Region.UKSouth;
            }
            else if (location.OrdinalEquals("wuk"))
            {
                return Region.UKWest;
            }
            else if (location.OrdinalEquals("cfr"))
            {
                return Region.FranceCentral;
            }
            else if (location.OrdinalEquals("sfr"))
            {
                return Region.FranceSouth;
            }
            else if (location.OrdinalEquals("nch"))
            {
                return Region.SwitzerlandNorth;
            }
            else if (location.OrdinalEquals("wch"))
            {
                return Region.SwitzerlandWest;
            }
            else if (location.OrdinalEquals("nde"))
            {
                return Region.GermanyNorth;
            }
            else if (location.OrdinalEquals("wcde"))
            {
                return Region.GermanyWestCentral;
            }
            else if (location.OrdinalEquals("wno"))
            {
                return Region.NorwayWest;
            }
            else if (location.OrdinalEquals("eno"))
            {
                return Region.NorwayEast;
            }
            else if (location.OrdinalEquals("hk"))
            {
                return Region.AsiaEast;
            }
            else if (location.OrdinalEquals("sing"))
            {
                return Region.AsiaSouthEast;
            }
            else if (location.OrdinalEquals("tyo"))
            {
                return Region.JapanEast;
            }
            else if (location.OrdinalEquals("wjp"))
            {
                return Region.JapanWest;
            }
            else if (location.OrdinalEquals("eau"))
            {
                return Region.AustraliaEast;
            }
            else if (location.OrdinalEquals("seau"))
            {
                return Region.AustraliaSouthEast;
            }
            else if (location.OrdinalEquals("cau"))
            {
                return Region.AustraliaCentral;
            }
            else if (location.OrdinalEquals("cau2"))
            {
                return Region.AustraliaCentral2;
            }
            else if (location.OrdinalEquals("cin"))
            {
                return Region.IndiaCentral;
            }
            else if (location.OrdinalEquals("sin"))
            {
                return Region.IndiaSouth;
            }
            else if (location.OrdinalEquals("win"))
            {
                return Region.IndiaWest;
            }
            else if (location.OrdinalEquals("skr"))
            {
                return Region.KoreaSouth;
            }
            else if (location.OrdinalEquals("sel"))
            {
                return Region.KoreaCentral;
            }
            else if (location.OrdinalEquals("cuae"))
            {
                return Region.UAECentral;
            }
            else if (location.OrdinalEquals("nuae"))
            {
                return Region.UAENorth;
            }
            else if (location.OrdinalEquals("nza"))
            {
                return Region.SouthAfricaNorth;
            }
            else if (location.OrdinalEquals("wza"))
            {
                return Region.SouthAfricaWest;
            }
            else if (location.OrdinalEquals("bj"))
            {
                return Region.ChinaNorth;
            }
            else if (location.OrdinalEquals("sha"))
            {
                return Region.ChinaEast;
            }
            else if (location.OrdinalEquals("bj2"))
            {
                return Region.ChinaNorth2;
            }
            else if (location.OrdinalEquals("sha2"))
            {
                return Region.ChinaEast2;
            }
            else if (location.OrdinalEquals("cde"))
            {
                return Region.GermanyCentral;
            }
            else if (location.OrdinalEquals("nede"))
            {
                return Region.GermanyNorthEast;
            }

            throw new ArgumentOutOfRangeException($"Short Azure region {location} is invalid.");
        }
    }
}
