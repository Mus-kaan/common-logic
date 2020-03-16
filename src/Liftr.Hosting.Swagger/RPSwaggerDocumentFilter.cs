//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// This filter will remove all definitions marked to exclusion from the swagger with the SwaggerExtensionAttribute.
    /// </summary>
    public class RPSwaggerDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc == null)
            {
                throw new ArgumentNullException(nameof(swaggerDoc));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            swaggerDoc.Components.Schemas = swaggerDoc.Components.Schemas
                .Where(schema => schema.Value.Format != "ShouldRemove").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}