//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// https://github.com/Azure/azure-resource-manager-rpc/blob/b6c13387234d93772b0689c0594370d30add6104/v1.0/common-api-details.md#proxy-request-header-modifications
    /// </summary>
    public static class ARMHeader
    {
        /// <summary>
        /// Always added. Set to the full URI that the client connected to (which will be different than the RP URI,
        /// since it will have the public hostname instead of the RP hostname).
        /// This value can be used in generating FQDN for Location headers or other requests since RPs should not reference their endpoint name.
        /// </summary>
        public const string Referer = "referer";

        /// <summary>
        /// Always added. Specifies the tracing correlation Id for the request;
        /// the resource provider *must* log this so that end-to-end requests can be correlated across Azure.
        /// </summary>
        public const string MSCorrelationRequestId = "x-ms-correlation-request-id";

        /// <summary>
        /// Always added . Set to the client IP address used in the request;
        /// this is required since the resource provider will not have access to the client IP.
        /// </summary>
        public const string MSClientIPAddress = "x-ms-client-ip-address";

        /// <summary>
        /// Always added. Set to the principal name / UPN of the client JWT making the request.
        /// </summary>
        public const string MSClientPrincipalName = "x-ms-client-principal-name";

        /// <summary>
        /// Added when available. Set to the principal Id of the client JWT making the request.
        /// Service principal will not have the principal Id.
        /// </summary>
        public const string MSClientPrincipalId = "x-ms-client-principal-id";

        /// <summary>
        /// Always added. Set to the tenant ID of the client JWT making the request.
        /// </summary>
        public const string MSTenantId = "x-ms-client-tenant-id";

        /// <summary>
        /// Always added. Set to the issuer of the client JWT making the request.
        /// </summary>
        public const string MSClientIssuer = "x-ms-client-issuer";

        /// <summary>
        /// Always added. Set to the object Id of the client JWT making the request.
        /// Not all users have object Id. For CSP (reseller) scenarios for example, object Id is not available.
        /// </summary>
        public const string MSClientObjectId = "x-ms-client-object-id";

        /// <summary>
        /// Always added. Set to the app Id of the client JWT making the request.
        /// </summary>
        public const string MSClientAppId = "x-ms-client-app-id";

        /// <summary>
        /// Always added. Set to the app Id acr claim of the client JWT making the request.
        /// This is the application authentication context class reference claim which indicates how the client was authenticated.
        /// </summary>
        public const string MSClientAppIdAcr = "x-ms-client-app-id-acr";

        /// <summary>
        /// Caller-specified value identifying the request, in the form of a GUID with no decoration
        /// such as curly braces (e.g. client-request-id: 9C4D50EE-2D56-4CD3-8152-34347DC9F2B0).
        /// If the caller provides this header – the resource provider *must* log this with their traces
        /// to facilitate tracing a single request.
        /// If specified, this will be included in response information as a way to map the request if "x-ms-return-client-request-id"; is specified as "true".
        /// </summary>
        public const string MSClientRequestId = "x-ms-client-request-id";

        /// <summary>
        /// Optional. True or false and indicates if a client-request-id should be included in the response. Default is false.
        /// </summary>
        public const string MSReturnClientRequestId = "x-ms-return-client-request-id";

        /// <summary>
        /// A unique identifier for the current operation, service generated.
        /// All the resource providers *must* return this value in the response headers to facilitate debugging.
        /// </summary>
        public const string MSRequestId = "x-ms-request-id";
    }
}
