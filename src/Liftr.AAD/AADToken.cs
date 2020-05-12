//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Liftr.AAD
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens
    /// </summary>
    public sealed class AADToken
    {
        private const string c_oid = "oid";
        private const string c_upn = "upn";
        private const string c_email = "email";
        private const string c_sub = "sub";

        private AADToken()
        {
        }

        /// <summary>
        /// The immutable identifier for an object in the Microsoft identity platform, in this case, a user account.
        /// It can also be used to perform authorization checks safely and as a key in database tables.
        /// This ID uniquely identifies the user across applications - two different applications signing in the same user will receive the same value in the oid claim.
        /// Thus, oid can be used when making queries to Microsoft online services, such as the Microsoft Graph.
        /// The Microsoft Graph will return this ID as the id property for a given user account.
        /// Because the oid allows multiple apps to correlate users, the profile scope is required in order to receive this claim.
        /// Note that if a single user exists in multiple tenants, the user will contain a different object ID in each tenant - they are considered different accounts, even though the user logs into each account with the same credentials.
        /// </summary>
        public string ObjectId { get; private set; }

        /// <summary>
        /// The username of the user. May be a phone number, email address, or unformatted string. Should only be used for display purposes and providing username hints in reauthentication scenarios.
        /// </summary>
        public string UPN { get; private set; }

        /// <summary>
        /// The principal about which the token asserts information, such as the user of an app. This value is immutable and cannot be reassigned or reused.
        /// It can be used to perform authorization checks safely, such as when the token is used to access a resource, and can be used as a key in database tables.
        /// Because the subject is always present in the tokens that Azure AD issues, we recommend using this value in a general-purpose authorization system.
        /// The subject is, however, a pairwise identifier - it is unique to a particular application ID.
        /// Therefore, if a single user signs into two different apps using two different client IDs, those apps will receive two different values for the subject claim.
        /// This may or may not be desired depending on your architecture and privacy requirements. See also the oid claim (which does remain the same across apps within a tenant).
        /// </summary>
        public string SUB { get; private set; }

        public string Email { get; private set; }

        public JwtPayload JwtPayload { get; private set; }

        public static AADToken FromJWT(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                throw new ArgumentNullException(nameof(jwt));
            }

            var result = new AADToken();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadToken(jwt) as JwtSecurityToken;
                var payload = token?.Payload as JwtPayload;
                if (payload == null)
                {
                    throw new InvalidAADTokenException("Cannot find payload in the JWT.");
                }

                result.JwtPayload = payload;

                // There is some extreme case that we will not get the oid from the token.
                if (payload.TryGet(c_oid, out string oid))
                {
                    result.ObjectId = oid;
                }

                if (payload.TryGet(c_upn, out string upn))
                {
                    result.UPN = upn;
                }

                if (payload.TryGet(c_email, out string email))
                {
                    result.Email = email;
                }

                if (payload.TryGet(c_sub, out string sub))
                {
                    result.SUB = sub;
                }
            }
            catch (Exception ex) when (!(ex is InvalidAADTokenException))
            {
                throw new InvalidAADTokenException("Invalid JWT.", ex);
            }

            return result;
        }
    }
}
