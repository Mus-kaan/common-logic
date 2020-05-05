//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Liftr.Utilities.Tests
{
    public class HttpBearerChallengeTests
    {
        public static TheoryData HttpBearerChallenge_ValidData
        {
            get
            {
                var data = new TheoryData<string, string, string, string, string, string>();

                data.Add(
                    "Bearer authorization_uri=\"https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47\", error=\"invalid_token\", error_description=\"The authentication failed because of missing 'Authorization' header.\"",
                    "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47",
                    "https://login.windows.net",
                    "72f988bf-86f1-41af-91ab-2d7cd011db47",
                    null,
                    null);

                data.Add(
                    "bearer authorization=\"https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47\", resource=\"testresource1\"",
                    "https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47",
                    "https://login.microsoftonline.com",
                    "72F988BF-86F1-41AF-91AB-2D7CD011DB47",
                    "testresource1",
                    null);

                data.Add(
                    "Bearer authorization=\"https://login.windows-ppe.net/5D929AE3-B37C-46AA-A3C8-C1558902F101\", resource=\"testresource2\", scope=\"scope2\"",
                    "https://login.windows-ppe.net/5D929AE3-B37C-46AA-A3C8-C1558902F101",
                    "https://login.windows-ppe.net",
                    "5D929AE3-B37C-46AA-A3C8-C1558902F101",
                    "testresource2",
                    "scope2");

                return data;
            }
        }

        public static TheoryData IsBearerChallenge_ValidData
        {
            get
            {
                var data = new TheoryData<string>();
                data.Add("Bearer authorization_uri=\"https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47\", error=\"invalid_token\", error_description=\"The authentication failed because of missing 'Authorization' header.\"");
                data.Add("Bearer authorization=\"https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47\", resource=\"https://serviceidentity.azure.net/\"");
                data.Add("bearer authorization=\"https://login.windows-ppe.net/5D929AE3-B37C-46AA-A3C8-C1558902F101\", resource=\"testreousrce\"");
                return data;
            }
        }

        public static TheoryData IsBearerChallenge_InvalidData
        {
            get
            {
                var data = new TheoryData<string>();
                data.Add("bearer2 authorization=\"https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47\", resource=\"https://serviceidentity.azure.net/\"");
                data.Add("authorization=\"testauth\", resource=\"testreousrce\"");
                data.Add("basic test challenge");
                data.Add("bearer test challenge");
                data.Add("bearer test=challenge");
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsBearerChallenge_ValidData))]
        public void IsBearerChallenge_PositiveTests(string challenge)
        {
            Assert.True(HttpBearerChallenge.TryParse(challenge, out _));
        }

        [Theory]
        [MemberData(nameof(IsBearerChallenge_InvalidData))]
        public void IsBearerChallenge_NegativeTests(string challenge)
        {
            Assert.False(HttpBearerChallenge.TryParse(challenge, out _));
        }

        [Theory]
        [MemberData(nameof(HttpBearerChallenge_ValidData))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>")]
        public void HttpBearerChallenge_PostiveTests(
            string challenge,
            string authorizationServer,
            string authenticationEndpoint,
            string tenantId,
            string resource,
            string scope)
        {
            HttpBearerChallenge bearerChallenge = null;
            Assert.True(HttpBearerChallenge.TryParse(challenge, out bearerChallenge));
            Assert.Equal(authorizationServer, bearerChallenge.AuthorizationAuthority.ToString());
            Assert.Equal(authenticationEndpoint, bearerChallenge.AuthenticationEndpoint);
            Assert.Equal(tenantId, bearerChallenge.TenantId);
            Assert.Equal(resource, bearerChallenge.Resource);
            Assert.Equal(scope, bearerChallenge.Scope);
        }
    }
}
