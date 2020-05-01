//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// The default response operation filter
    /// </summary>
    public class DefaultResponseOperationFilter : IOperationFilter
    {
        private const string c_default = "default";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var defaultResponseAttributes = context.MethodInfo.GetCustomAttributes(true).OfType<SwaggerDefaultResponseAttribute>();

            var schemaRepository = context.SchemaRepository;

            foreach (var attr in defaultResponseAttributes)
            {
                var defaultResponse = new OpenApiResponse()
                {
                    Description = attr.Description,
                };

                if (attr.Type != null)
                {
                    var attrSchema = context.SchemaGenerator.GenerateSchema(attr.Type, schemaRepository);
                    defaultResponse.Content = new Dictionary<string, OpenApiMediaType>();
                    defaultResponse.Content["application/json"] = new OpenApiMediaType() { Schema = attrSchema };
                }

                operation.Responses[c_default] = defaultResponse;
            }

            // ARM (Auto Rest) requires a default response for each API to handle error cases.
            if (!operation.Responses.ContainsKey(c_default))
            {
                var responseSchema = context.SchemaGenerator.GenerateSchema(
                    typeof(ResourceProviderDefaultErrorResponse), schemaRepository);

                var defaultResponse = new OpenApiResponse()
                {
                    Description = "Default error response.",
                    Content = new Dictionary<string, OpenApiMediaType>(),
                };

                defaultResponse.Content["application/json"] = new OpenApiMediaType() { Schema = responseSchema };

                operation.Responses[c_default] = defaultResponse;
            }
        }
    }
}
