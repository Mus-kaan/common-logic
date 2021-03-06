//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.AAD;
using Xunit;

namespace Microsoft.Liftr.TokenManager.Tests
{
    public class AADTokenParsingTests
    {
        private const string c_oid = "de4f0bcb-480a-45b2-896f-41cb0569e6f3";
        private static readonly string s_inputContent = "ZXlKMGVYQWlPaUpLVjFRaUxDSmhiR2NpT2lKU1V6STFOaUlzSW5nMWRDSTZJblZtYmtNdGIyWkdYM0ZmUTBOVGFWRk1iMk01YTFGTGNWb3dRU0lzSW10cFpDSTZJblZtYmtNdGIyWkdYM0ZmUTBOVGFWRk1iMk01YTFGTGNWb3dRU0o5LmV5SmhkV1FpT2lKb2RIUndjem92TDIxaGJtRm5aVzFsYm5RdVkyOXlaUzUzYVc1a2IzZHpMbTVsZEM4aUxDSnBjM01pT2lKb2RIUndjem92TDNOMGN5NTNhVzVrYjNkekxYQndaUzV1WlhRdlpqWTRObVEwTWpZdE9HUXhOaTAwTW1SaUxUZ3hZamN0WVdJMU56aGxNVEV3WTJOa0x5SXNJbWxoZENJNk1UVTRPVEl6TkRneE1pd2libUptSWpveE5UZzVNak0wT0RFeUxDSmxlSEFpT2pFMU9Ea3lNemczTVRJc0lsOWpiR0ZwYlY5dVlXMWxjeUk2ZXlKbmNtOTFjSE1pT2lKemNtTXhJbjBzSWw5amJHRnBiVjl6YjNWeVkyVnpJanA3SW5OeVl6RWlPbnNpWlc1a2NHOXBiblFpT2lKb2RIUndjem92TDJkeVlYQm9MbkJ3WlM1M2FXNWtiM2R6TG01bGRDOW1OamcyWkRReU5pMDRaREUyTFRReVpHSXRPREZpTnkxaFlqVTNPR1V4TVRCalkyUXZkWE5sY25NdlpHVTBaakJpWTJJdE5EZ3dZUzAwTldJeUxUZzVObVl0TkRGallqQTFOamxsTm1ZekwyZGxkRTFsYldKbGNrOWlhbVZqZEhNaWZYMHNJbUZqY2lJNklqRWlMQ0poYVc4aU9pSkJWRkZCZVM4NFUwRkJRVUZuUkVGVlRXazRhbkJhYWxwUllrVTBVM2hoTUV0SFFWRnRNREp0WXpJdk5EbEhNVFJZWTJZMWJXeHNaMGhFUkRCWFUxbG9ZbTExYm1SdWNrODFiRFIzSWl3aVlXMXlJanBiSW5kcFlTSmRMQ0poY0hCcFpDSTZJbU0wTkdJME1EZ3pMVE5pWWpBdE5EbGpNUzFpTkRka0xUazNOR1UxTTJOaVpHWXpZeUlzSW1Gd2NHbGtZV055SWpvaU1pSXNJbVpoYldsc2VWOXVZVzFsSWpvaVYyVnVaeUlzSW1kcGRtVnVYMjVoYldVaU9pSlhkWGxwSWl3aWFXNWZZMjl5Y0NJNkluUnlkV1VpTENKcGNHRmtaSElpT2lJMU1DNDBOeTR4TURZdU1qTTRJaXdpYm1GdFpTSTZJbGQxZVdrZ1YyVnVaeUlzSW05cFpDSTZJbVJsTkdZd1ltTmlMVFE0TUdFdE5EVmlNaTA0T1RabUxUUXhZMkl3TlRZNVpUWm1NeUlzSW05dWNISmxiVjl6YVdRaU9pSlRMVEV0TlMweU1TMHlNVEkzTlRJeE1UZzBMVEUyTURRd01USTVNakF0TVRnNE56a3lOelV5TnkweE56UTVNekl3TUNJc0luQjFhV1FpT2lJeE1EQXpRa1pHUkRreE0wWTJORFl6SWl3aWMyTndJam9pZFhObGNsOXBiWEJsY25OdmJtRjBhVzl1SWl3aWMzVmlJam9pYzNRMFpVSmxlbGxaU1cweFpXWlBZMDlZYm1KQ2RrcG9lV2xmYUd0eGJ6QlpMV2RvVWsxbWFGSkVUU0lzSW5ScFpDSTZJbVkyT0Raa05ESTJMVGhrTVRZdE5ESmtZaTA0TVdJM0xXRmlOVGM0WlRFeE1HTmpaQ0lzSW5WdWFYRjFaVjl1WVcxbElqb2lkM1YzWlc1blFHMXBZM0p2YzI5bWRDNWpiMjBpTENKMWNHNGlPaUozZFhkbGJtZEFiV2xqY205emIyWjBMbU52YlNJc0luVjBhU0k2SW1kMVFsVmFlR2c0TkZVeVQwdGplRXBoWjFWRlFVRWlMQ0oyWlhJaU9pSXhMakFpZlEuV0pIc0Rma2F6MzNvZG9DR3RsSzctN3FSU2syRXZrdXlzSWJSSEZmdzkydEI2UDlkSWJmNlpLU0VhMTQwWkhWNF9QVFV0eVpFZWZISW9vU3hUQk4tMUxqQ2JHcV8xald0dkZYdlVnZTBSRFlCR1BwX19ZbG95TnA2b25vV2dwZFFhREV3VEpjTlBnNW03YVl6OXl1dGg4VHZDd0xvQVFWMzZOY2dmQTVkaThjOHhLZTB5NHVKalJOSlhSZDZjUDJrT3dTMmV5SG5xRjBfa3FGbWE3Tl9ENWFXVGliallMN1hEdnZoVjZPZDRLQ0lMZElyZXd0SXVHb3pwMWxlWVd3UHlTa09YSjNORHUtVGJCYXRaTEFVeVNWenNmaXAwa3I4UmlsUzZPOUZsekk0VWVmb0NJa3hXeXhCLWtReTFCbXQtakVnUFhXWHozdHZVdnM5MXdRbFlR".FromBase64();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        [Fact]
        public void VerifyParsingToken()
        {
            var parsedJWT = AADToken.FromJWT(s_inputContent);
            Assert.Equal(c_oid, parsedJWT.ObjectId);
        }
    }
}
