//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Liftr.JwtLibrary
{
    public interface IJwtTokenValidator
    {
        /// <summary>
        /// Validates a JWT token. If successful, will return the JWT payload data.
        /// </summary>
        JwtPayload ValidateJwtToken(string token, string issuer, string audience);
    }
}
