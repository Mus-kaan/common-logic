//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// The default response operation filter
    /// </summary>
    public class DefaultResponseOperationFilter : IOperationFilter
    {
        private const string c_default = "default";

        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var schemaRegistry = context.SchemaRegistry ?? throw new ArgumentException(nameof(context.SchemaRegistry));

            var defaultResponseAttributes = context.MethodInfo.GetCustomAttributes(true).OfType<SwaggerDefaultResponseAttribute>();

            foreach (var attr in defaultResponseAttributes)
            {
                var defaultResponse = new Response
                {
                    Description = attr.Description,
                    Schema = (attr.Type != null) ? schemaRegistry.GetOrRegister(attr.Type) : null,
                };

                operation.Responses[c_default] = defaultResponse;
            }

            // ARM (Auto Rest) requires a default response for each API to handle error cases.
            if (!operation.Responses.ContainsKey(c_default))
            {
                var defaultResponse = new Response
                {
                    Description = "Default error response.",
                    Schema = schemaRegistry.GetOrRegister(typeof(ResourceProviderDefaultErrorResponse)),
                };
                operation.Responses[c_default] = defaultResponse;
            }
        }
    }
}
