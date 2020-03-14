//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// This filter will set the x-ms-pageable extension to all operations whose ID contains "List",
    /// and also set the x-ms-long-running-operation to all operations containing 201 and 202 as response status.
    /// Remember to enable annotations by calling c.EnableAnnotations(); in order for this filter to have effect.
    /// </summary>
    public class RPSwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.OperationId.Contains("List"))
            {
                operation.Extensions.Add("x-ms-pageable", new OpenApiObject()
                {
                    ["nextLinkName"] = new OpenApiString("nextLink"),
                });
            }

            if (operation.Responses.ContainsKey("201") || operation.Responses.ContainsKey("202"))
            {
                operation.Extensions.Add("x-ms-long-running-operation", new OpenApiBoolean(true));
            }
        }
    }
}
