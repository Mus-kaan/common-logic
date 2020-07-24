//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.JwtLibrary
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        /// <summary>
        /// When creating a new token, we must "skew" the time backwards at which it's been
        /// created so that they are valid even if the verifier has no clock-skewing
        /// in place. By default we "issue" the token 15 minutes in the past.
        /// </summary>
        private static readonly TimeSpan s_TokenIssuedAtClockSkew = TimeSpan.FromMinutes(-15);

        private ConcurrentDictionary<string, SigningCredentials> _signingCredentials = new ConcurrentDictionary<string, SigningCredentials>();

        private ILogger _logger;

        public JwtTokenGenerator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task AddKeyAsync(string privateKeyPath, string privateKeyPassword)
        {
            using (var certFile = File.OpenRead(privateKeyPath))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                AddKey(buff, privateKeyPassword);
            }
        }

        public void AddKey(byte[] certBytes, string privateKeyPassword)
        {
            using (var tokenSigningCertificate = new X509Certificate2(certBytes, privateKeyPassword))
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
            var key = new RsaSecurityKey(cert.GetRSAPrivateKey());
            var signingCredential = new SigningCredentials(key, "RS256");
            if (!_signingCredentials.TryAdd(signingKid, signingCredential))
            {
                _logger.Warning("warn_fail_add_generator_key. {kid}", signingKid);
            }
        }

        public async Task RemoveKeyAsync(string privateKeyPath, string privateKeyPassword)
        {
            using (var certFile = File.OpenRead(privateKeyPath))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                RemoveKey(buff, privateKeyPassword);
            }
        }

        public void RemoveKey(byte[] certBytes, string privateKeyPassword)
        {
            using (var tokenSigningCertificate = new X509Certificate2(certBytes, privateKeyPassword))
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

            // primary key, for signing and validation
            var signingKid = cert.Thumbprint;
            if (!_signingCredentials.TryRemove(signingKid, out var key))
            {
                _logger.Warning("warn_fail_remove_generator_key. {kid}", signingKid);
            }
        }

        /// <summary>
        /// Create a new JwtPayload used by Liftr token scenarios
        /// The token has basic properties, like an issuer, an audience, notBefore time, expires time, issuedAt time, jti claim with a GUID,
        /// and grant_type
        /// </summary>
        /// <param name="issuer">Token issuer (who created and signed this token)</param>
        /// <param name="audience">Audience (who the token is intended for)</param>
        /// <param name="subject">Subject (whom the token refers to)</param>
        /// <param name="expiration">Expiration time (seconds since Unix epoch)</param>
        /// <returns>A new JwtPayload instance with the basic fields filled out.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "<Pending>")]
        public JwtPayload NewJwtPayload(
            string issuer,
            string audience,
            string subject,
            DateTime expiration)
        {
            var issuedAt = DateTime.UtcNow.Add(s_TokenIssuedAtClockSkew);
            var jwtid = Guid.NewGuid().ToString();
            var payload = new JwtPayload(
                issuer: issuer,
                audience: audience,
                claims: new Claim[]
                {
                    new Claim("jti", jwtid),
                    new Claim("sub", subject),
                },
                notBefore: issuedAt,
                expires: expiration,
                issuedAt: issuedAt);
            payload.Add("version", "1.0");
            payload.Add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
            return payload;
        }

        /// <summary>
        /// Generate a JWT token using the default key
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public string GetJwtToken(JwtPayload payload)
        {
            if (_signingCredentials.Count == 0)
            {
                _logger.Error("err_no_key_exists");
                throw new KeyNotFoundException("err_no_key_exists");
            }

            var kid = _signingCredentials.Keys.First();
            return GetJwtToken(kid, payload);
        }

        /// <summary>
        /// Takes a JwtPayload instance and creates a secure token signed with the configured certificate
        /// and indicating the currently configured "kid" in the header.
        /// </summary>
        public string GetJwtToken(string kid, JwtPayload payload)
        {
            if (!_signingCredentials.ContainsKey(kid))
            {
                _logger.Error("err_no_kid_found: {Kid}", kid);
                throw new ArgumentException("err_no_kid_found", nameof(kid));
            }

            var header = new JwtHeader(_signingCredentials[kid]);
            header.Add("kid", kid);
            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
