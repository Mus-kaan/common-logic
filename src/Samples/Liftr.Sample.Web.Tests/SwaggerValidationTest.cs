//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;

namespace Liftr.Sample.Web.Tests
{
    public class SwaggerValidationTest
    {
        [Fact]
        public void SwaggerContracts()
        {
            var expectingSwagger = File.ReadAllText("expecting-swagger.json");
            var generatedSwagger = File.ReadAllText("swagger.json");

            // Remove new line to avoid the different '\r\n' or '\n'.
            expectingSwagger = RemoveNewLine(expectingSwagger);
            generatedSwagger = RemoveNewLine(generatedSwagger);
            Assert.Equal(expectingSwagger, generatedSwagger);
        }

        private static string RemoveNewLine(string input)
        {
            dynamic obj = JObject.Parse(input);
            return JsonConvert.SerializeObject(obj, Formatting.None);
        }
    }
}
