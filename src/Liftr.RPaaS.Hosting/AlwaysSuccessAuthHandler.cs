//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS.Hosting
{
    public class AlwaysSuccessAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AlwaysSuccessAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    public class AlwaysSuccessAuthorizationHandler : AuthorizationHandler<AlwaysSuccessRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AlwaysSuccessRequirement requirement)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    public class AlwaysSuccessRequirement : IAuthorizationRequirement
    {
    }
}
