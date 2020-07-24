//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Liftr.JwtLibrary
{
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Create a new JwtPayload instance with the basic fields filled out. In particular, it ensures to have an
        /// issuer, an audience, notBefore time, expires time, issuedAt time and a jti claim with a GUID.
        /// </summary>
        /// <param name="issuer">Token issuer (who created and signed this token)</param>
        /// <param name="audience">Audience (who the token is intended for)</param>
        /// <param name="subject">Subject (whom the token refers to)</param>
        /// <param name="expiration">Expiration time (seconds since Unix epoch)</param>
        /// <returns>A new JwtPayload instance with the basic fields filled out.</returns>
        JwtPayload NewJwtPayload(
            string issuer,
            string audience,
            string subject,
            DateTime expiration);

        /// <summary>
        /// Takes a JwtPayload instance and creates a secure token signed with the configured certificate
        /// and indicating the currently configured "kid" in the header.
        /// </summary>
        string GetJwtToken(JwtPayload payload);
    }
}
