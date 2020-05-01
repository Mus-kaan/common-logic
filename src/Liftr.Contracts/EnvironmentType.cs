//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr
{
    public enum EnvironmentType
    {
        Production,
        Canary,
        DogFood,
        Dev,
        Test,
        Fairfax,
        Mooncake,
    }

    public static class EnvironmentTypeExtensions
    {
        public static string ShortName(this EnvironmentType env)
        {
            switch (env)
            {
                case EnvironmentType.Production:
                    return "prod";
                case EnvironmentType.Canary:
                    return "euap";
                case EnvironmentType.DogFood:
                    return "df";
                case EnvironmentType.Dev:
                    return "dev";
                case EnvironmentType.Test:
                    return "test";
                case EnvironmentType.Fairfax:
                    return "ff";
                case EnvironmentType.Mooncake:
                    return "mc";
                default:
                    throw new ArgumentOutOfRangeException(nameof(env));
            }
        }
    }
}
