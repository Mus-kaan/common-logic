//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Swashbuckle.AspNetCore.Swagger;
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
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc == null)
            {
                throw new ArgumentNullException(nameof(swaggerDoc));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            swaggerDoc.Definitions = swaggerDoc.Definitions
                .Where(def => def.Value.Format != "ShouldRemove").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}