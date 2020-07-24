//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.JwtLibrary
{
    public static class Constants
    {
        public const string LiftrTokenService = "Liftr Token Service";

        public enum LiftrServiceNames
        {
            Default,
            TokenService,
            BillingService,
            WebhookService,
        }
    }
}
