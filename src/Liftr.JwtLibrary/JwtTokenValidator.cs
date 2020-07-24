//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.JwtLibrary
{
    public class JwtTokenValidator : IJwtTokenValidator
    {
        private readonly TimeSpan _MaxAllowedTimeShift = TimeSpan.FromSeconds(15);
        private ConcurrentDictionary<string, SecurityKey[]> _signingKeys = new ConcurrentDictionary<string, SecurityKey[]>();
        private ILogger _logger;

        public JwtTokenValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task AddKeyAsync(string certPath)
        {
            using (var certFile = File.OpenRead(certPath))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                AddKey(buff);
            }
        }

        public void AddKey(byte[] certBytes)
        {
            using (var tokenSigningCertificate = new X509Certificate2(certBytes))
            {
                AddKey(tokenSigningCertificate);
            }
        }

        public void AddKey(X509Certificate2 cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException(nameof(cert));
            }

            var signingKid = cert.Thumbprint;
            if (!_signingKeys.ContainsKey(signingKid))
            {
                var key = new RsaSecurityKey(cert.GetRSAPublicKey());
                if (_signingKeys.TryAdd(signingKid, new SecurityKey[] { key }))
                {
                    _logger.Warning("warn_fail_add_validator_key. {kid}", signingKid);
                }
            }
        }

        public async Task RemoveKeyAsync(string certPath)
        {
            using (var certFile = File.OpenRead(certPath))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                RemoveKey(buff);
            }
        }

        public void RemoveKey(byte[] certBytes)
        {
            using (var tokenSigningCertificate = new X509Certificate2(certBytes))
            {
                RemoveKey(tokenSigningCertificate);
            }
        }

        public void RemoveKey(X509Certificate2 cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException(nameof(cert));
            }

            var signingKid = cert.Thumbprint;
            if (!_signingKeys.ContainsKey(signingKid))
            {
                var key = new RsaSecurityKey(cert.GetRSAPublicKey());
                if (_signingKeys.TryAdd(signingKid, new SecurityKey[] { key }))
                {
                    _logger.Warning("warn_fail_add_validator_key. {kid}", signingKid);
                }
            }
        }

        /// <summary>
        /// Validates a JWT token. If successful, will return the JWT payload data.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public JwtPayload ValidateJwtToken(string token, string issuer, string audience)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();

            /*
                MaxAllowedTimeShift extends the token expiration time for a very short period of time.
                It helps to address two issues:
                1. Small clock skew between client and registry. Client sees the token still valid while registry sees the token expired.
                2. Right before the token expires, client sends request, while it expires when reaches registry. Docker client doesn't
                handle this case so we have to fix it on our side.
            */
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = audience,
                ValidIssuer = issuer,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = CustomSigningKeyResolver,
                ClockSkew = _MaxAllowedTimeShift,
            };
            try
            {
                SecurityToken validatedToken;
                handler.ValidateToken(token, validationParameters, out validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;
                return jwtToken?.Payload as JwtPayload;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a set of security keys that will be used to validate a JWT token.
        /// It only returns the keys if the kid provided matches the signing kid.
        /// Note that this needs to be replaced by a comparison to a set of signing
        /// kids in the future when we expand to support multiple keys.
        /// </summary>
        private SecurityKey[] CustomSigningKeyResolver(string tkn, SecurityToken stkn, string kid, TokenValidationParameters vp)
        {
            if (_signingKeys.ContainsKey(kid))
            {
                return _signingKeys[kid];
            }

            _logger.Error("err_no_kid_resoloved: {Kid}", kid);
            return null;
        }
    }
}
