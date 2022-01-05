//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.IdempotentRPWorker.Constants
{
    public static class RPHeaderConstants
    {
        public const string ClientObjectIdKey = "x-ms-client-object-id";
        public const string ClientTenantIdKey = "x-ms-client-tenant-id";
        public const string ClientIssuerKey = "x-ms-client-issuer";
        public const string ClientPrincipalIdKey = "x-ms-client-principal-id";
        public const string ClientPrincipalNameKey = "x-ms-client-principal-name";
        public const string ClientGroupMembershipKey = "x-ms-client-principal-group-membership";
        public const string IdentityUrlKey = "x-ms-identity-url";
        public const string IdentityPrincipalIdKey = "x-ms-identity-principal-id";
        public const string HomeTenantIdKey = "x-ms-home-tenant-id";
    }
}
