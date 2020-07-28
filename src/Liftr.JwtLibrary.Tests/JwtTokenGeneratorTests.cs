//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.JwtLibrary.Utilities;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.JwtLibrary.Tests
{
    public class JwtTokenGeneratorTests
    {
        private const string certfile1 = "JwtTokenGeneratorTests_c1.cer";
        private const string pkfile1 = "JwtTokenGeneratorTests_c1.pfx";
        private const string alias1 = "alias1";
        private const string password1 = "password1";

        private const string certfile2 = "JwtTokenGeneratorTests_c2.cer";
        private const string pkfile2 = "JwtTokenGeneratorTests_c2.pfx";
        private const string alias2 = "alias2";
        private const string password2 = "password2";

        private const string certfile3 = "JwtTokenGeneratorTests_c3.cer";
        private const string pkfile3 = "JwtTokenGeneratorTests_c3.pfx";
        private const string alias3 = "alias3";
        private const string password3 = ""; // empty password

        public JwtTokenGeneratorTests()
        {
            JwtTokenHelpers.GenerateCertificateAndPrivateKey(certfile1, pkfile1, alias1, password1);

            JwtTokenHelpers.GenerateCertificateAndPrivateKey(certfile2, pkfile2, alias2, password2);

            JwtTokenHelpers.GenerateCertificateAndPrivateKey(certfile3, pkfile3, alias3, password3);
        }

        [Fact]
        public async Task ValidateTokenGeneratedByValidKeyAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = "issuer";
            var audience = "audience";
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var token = tg_1.GetJwtToken(payload);

            // user cert 1 to validate
            var tv_1 = new JwtTokenValidator(logger);
            await tv_1.AddKeyAsync(certfile1);
            var validPayload = tv_1.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.NotNull(validPayload);
            string jti1, jti2;
            Assert.True(payload.TryGet("jti", out jti1));
            Assert.True(validPayload.TryGet("jti", out jti2));
            Assert.Equal(jti1, jti2);
        }

        [Fact]
        public async Task ValidateTokenGeneratedByAnyValidKeyAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = "issuer";
            var audience = "audience";
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var token = tg_1.GetJwtToken(payload);

            // user cert 1 to validate
            var tv_1 = new JwtTokenValidator(logger);
            await tv_1.AddKeyAsync(certfile3);
            await tv_1.AddKeyAsync(certfile2);
            await tv_1.AddKeyAsync(certfile1);
            var validPayload = tv_1.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.NotNull(validPayload);
            string jti1, jti2;
            Assert.True(payload.TryGet("jti", out jti1));
            Assert.True(validPayload.TryGet("jti", out jti2));
            Assert.Equal(jti1, jti2);
        }

        [Fact]
        public async Task ValidateTokenInvalidIssuerAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = "issuer";
            var audience = "audience";
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var token = tg_1.GetJwtToken(payload);

            // user cert 1 to validate with bad issuer
            var tv_1 = new JwtTokenValidator(logger);
            await tv_1.AddKeyAsync(certfile1);
            var validPayload = tv_1.ValidateJwtToken(token, "badissuer", audience);

            // validation
            Assert.Null(validPayload);
        }

        [Fact]
        public async Task ValidateTokenGeneratedByInvalidKeyAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = "issuer";
            var audience = "audience";
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var token = tg_1.GetJwtToken(payload);

            // use cert 2 to validate
            var tv_2 = new JwtTokenValidator(logger);
            await tv_2.AddKeyAsync(certfile2);
            var validPayload = tv_2.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.Null(validPayload);
        }

        [Fact]
        public async Task ValidateTokenGeneratedByCertBufferAsync()
        {
            var logger = Log.Logger;

            // use key 3 to generate the token
            var tg_3 = new JwtTokenGenerator(logger);
            using (var certFile = File.OpenRead(pkfile3))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                tg_3.AddKey(buff, null);
            }

            var issuer = "issuer";
            var audience = "audience";
            var subject = "subject";
            var payload = tg_3.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var token = tg_3.GetJwtToken(payload);

            // use cert 3 to validate
            var tv_3 = new JwtTokenValidator(logger);
            using (var certFile = File.OpenRead(certfile3))
            {
                var len = certFile.Length;
                var buff = new byte[len];
                await certFile.ReadAsync(buff, 0, (int)len, CancellationToken.None);
                tv_3.AddKey(buff);
            }

            var validPayload = tv_3.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.NotNull(validPayload);
            string jti1, jti2;
            Assert.True(payload.TryGet("jti", out jti1));
            Assert.True(validPayload.TryGet("jti", out jti2));
            Assert.Equal(jti1, jti2);
        }
    }
}
