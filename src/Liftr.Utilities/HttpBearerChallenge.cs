//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Utilities
{
    /// <summary>
    /// The Http Bearer Challenge.
    /// The class implementation is taken and modified from:
    /// https://github.com/Azure/azure-sdk-for-net/blob/3f736b5af3851ab99bbaa98483ae537de0d48cfb/src/SDKs/KeyVault/dataPlane/Microsoft.Azure.KeyVault/Customized/Authentication/HttpBearerChallenge.cs
    /// https://msazure.visualstudio.com/One/_git/Azure-Express?path=%2Fsrc%2Fdev%2FServices%2FExpress%2FServer.Common%2FAuthentication%2FManagedIdentity%2FHttpBearerChallenge.cs
    /// </summary>
    public class HttpBearerChallenge
    {
        /// <summary>
        /// The bearer claim.
        /// </summary>
        private const string c_Bearer = "Bearer";

        /// <summary>
        /// The bearer header prefix.
        /// </summary>
        private const string c_BearerHeaderPrefix = c_Bearer + " ";

        /// <summary>
        /// The authorization challenge parameter.
        /// </summary>
        private const string c_Authorization = "authorization";

        /// <summary>
        /// The tenant Id index in path segments.
        /// </summary>
        private const int c_TenantIdIndex = 1;

        /// <summary>
        /// The index of the key when split on <see cref="c_ChallengeKeyValueSeparator"/>
        /// </summary>
        private const int c_ChallengeKeyIndex = 0;

        /// <summary>
        /// The index of the value when split on <see cref="c_ChallengeKeyValueSeparator"/>
        /// </summary>
        private const int c_ChallengeValueIndex = 1;

        /// <summary>
        /// The separator for different challenge values.
        /// </summary>
        private const string c_ChallengeSeparator = ",";

        /// <summary>
        /// The separator for challenge key and value.
        /// </summary>
        private const string c_ChallengeKeyValueSeparator = "=";

        /// <summary>
        /// The authorization Uri challenge parameter.
        /// </summary>
        private const string c_AuthorizationUri = "authorization_uri";

        /// <summary>
        /// The resource key.
        /// </summary>
        private const string c_ResourceKey = "resource";

        /// <summary>
        /// The scope key.
        /// </summary>
        private const string c_ScopeKey = "scope";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBearerChallenge" /> class.
        /// </summary>
        /// <param name="authorizationAuthority">The authorization authority.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="scope">The scope</param>
        private HttpBearerChallenge(Uri authorizationAuthority, string resource, string scope)
        {
            if (authorizationAuthority == null)
            {
                throw new ArgumentNullException(nameof(authorizationAuthority));
            }

            AuthorizationAuthority = authorizationAuthority;
            Resource = resource;
            Scope = scope;

            TenantId = AuthorizationAuthority.Segments[c_TenantIdIndex];
            AuthenticationEndpoint = AuthorizationAuthority.GetLeftPart(UriPartial.Authority);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        public Uri AuthorizationAuthority { get; private set; }

        /// <summary>
        /// Gets the tenant Id.
        /// </summary>
        public string TenantId { get; private set; }

        /// <summary>
        /// The authentication endpoint.
        /// </summary>
        public string AuthenticationEndpoint { get; private set; }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        public string Resource { get; private set; }

        /// <summary>
        /// Returns the Scope value if present, otherwise string.Empty
        /// </summary>
        public string Scope { get; private set; }

        /// <summary>
        /// Parses the given challenge into Http bearer challenge.
        /// This method does not throw.
        /// Sample: Bearer authorization="https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47", resource="https://serviceidentity.azure.net/"
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <param name="httpBearerChallenge">The Http bearer challenge.</param>
        /// <returns>True if successful in parsing else false.</returns>
        public static bool TryParse(string challenge, out HttpBearerChallenge httpBearerChallenge)
        {
            if (!string.IsNullOrEmpty(challenge))
            {
                if (challenge.Trim().StartsWith(c_BearerHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    IDictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string trimmedChallenge = challenge.Substring(HttpBearerChallenge.c_Bearer.Length + 1);

                    string[] pairs = trimmedChallenge.Split(new string[] { HttpBearerChallenge.c_ChallengeSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    if (pairs != null && pairs.Length > 0)
                    {
                        foreach (string pair in pairs)
                        {
                            if (pair == null)
                            {
                                continue;
                            }

                            string[] keyValue = pair.Split(new string[] { HttpBearerChallenge.c_ChallengeKeyValueSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            if (keyValue.Length == 2)
                            {
                                string key = HttpBearerChallenge.TrimBearerChallengeValue(keyValue[HttpBearerChallenge.c_ChallengeKeyIndex]);
                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    parameters[key] = HttpBearerChallenge.TrimBearerChallengeValue(keyValue[HttpBearerChallenge.c_ChallengeValueIndex]);
                                }
                            }
                        }
                    }

                    string authority = null;
                    string resource = null;
                    string scope = null;
                    if (parameters.ContainsKey(HttpBearerChallenge.c_Authorization))
                    {
                        authority = parameters[HttpBearerChallenge.c_Authorization];
                    }
                    else if (parameters.ContainsKey(HttpBearerChallenge.c_AuthorizationUri))
                    {
                        authority = parameters[HttpBearerChallenge.c_AuthorizationUri];
                    }

                    if (parameters.ContainsKey(HttpBearerChallenge.c_ResourceKey))
                    {
                        resource = parameters[HttpBearerChallenge.c_ResourceKey];
                    }

                    if (parameters.ContainsKey(HttpBearerChallenge.c_ScopeKey))
                    {
                        scope = parameters[HttpBearerChallenge.c_ScopeKey];
                    }

                    if (!string.IsNullOrWhiteSpace(authority) &&
                        Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
                    {
                        httpBearerChallenge = new HttpBearerChallenge(authorityUri, resource, scope);
                        return true;
                    }
                }
            }

            httpBearerChallenge = null;
            return false;
        }

        /// <summary>
        /// Trims the bearer challenge key or value.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The trimmed value.</returns>
        private static string TrimBearerChallengeValue(string input)
        {
            if (input != null)
            {
                return input.Trim().Trim(new char[] { '\"' });
            }

            return null;
        }
    }
}
