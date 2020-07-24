//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.JwtLibrary.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Liftr.JwtLibrary.Tests
{
    public class JwtTokenExtensionsTests
    {
        private const string certfile1 = "JwtTokenExtensionsTests_c1.cer";
        private const string pkfile1 = "JwtTokenExtensionsTestsc1.pfx";
        private const string alias1 = "alias1";
        private const string password1 = "password1";

        public JwtTokenExtensionsTests()
        {
            JwtTokenHelpers.GenerateCertificateAndPrivateKey(certfile1, pkfile1, alias1, password1);
        }

        [Fact]
        public async Task ValidateTokenValidAccessCheckAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = Constants.LiftrTokenService;
            var audience = Constants.LiftrServiceNames.BillingService.ToString();
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var access = new List<ResourceScope>();
            access.Add(new ResourceScope()
            {
                Type = TokenRequestTypes.LiftrBillingToken,
                Name = "myresourceId",
                Actions = new string[] { ResourceScope.ReadAction, ResourceScope.WriteAction },
            });
            payload.AddAccess(access);
            var token = tg_1.GetJwtToken(payload);

            // user cert 1 to validate
            var tv_1 = new JwtTokenValidator(logger);
            await tv_1.AddKeyAsync(certfile1);
            var validPayload = tv_1.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.NotNull(validPayload);
            var accessCheck = validPayload.ValidateAccess(TokenRequestTypes.LiftrBillingToken, ResourceScope.WriteAction);
            Assert.True(accessCheck);
        }

        [Fact]
        public async Task ValidateTokenInvalidAccessCheckAsync()
        {
            var logger = Log.Logger;

            // use key 1 to generate the token
            var tg_1 = new JwtTokenGenerator(logger);
            await tg_1.AddKeyAsync(pkfile1, password1);
            var issuer = Constants.LiftrTokenService;
            var audience = Constants.LiftrServiceNames.BillingService.ToString();
            var subject = "subject";
            var payload = tg_1.NewJwtPayload(issuer, audience, subject, DateTime.UtcNow.AddHours(1));
            var access = new List<ResourceScope>();
            access.Add(new ResourceScope()
            {
                Type = TokenRequestTypes.LiftrBillingToken,
                Name = "myresourceId",
                Actions = new string[] { ResourceScope.ReadAction, ResourceScope.WriteAction },
            });
            payload.AddAccess(access);
            var token = tg_1.GetJwtToken(payload);

            // user cert 1 to validate
            var tv_1 = new JwtTokenValidator(logger);
            await tv_1.AddKeyAsync(certfile1);
            var validPayload = tv_1.ValidateJwtToken(token, issuer, audience);

            // validation
            Assert.NotNull(validPayload);
            var accessCheck = validPayload.ValidateAccess(TokenRequestTypes.LiftrBillingToken, ResourceScope.DeleteAction);
            Assert.False(accessCheck);
        }
    }
}
