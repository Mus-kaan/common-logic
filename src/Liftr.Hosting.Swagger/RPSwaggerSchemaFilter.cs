//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Hosting.Swagger
{
    /// <summary>
    /// This filter will exclude/mark as readonly the properties marked with the SwaggerExtensionAttribute.
    /// Also, the definitions marked to be excluded will have the format set to "ShouldRemove". This means that
    /// the RPSwaggerDocumentFilter should be applied as well, otherwise the swagger will have some not-desired formats.
    /// </summary>
    public class RPSwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Type != null)
            {
                var systemTypeAttributes = context.Type.GetCustomAttributes(true);

                foreach (Attribute attribute in systemTypeAttributes)
                {
                    if (attribute is SwaggerExtensionAttribute)
                    {
                        var swaggerAttribute = attribute as SwaggerExtensionAttribute;

                        if (swaggerAttribute.ExcludeFromSwagger)
                        {
                            schema.Format = "ShouldRemove";
                        }

                        if (swaggerAttribute.MarkAsReadOnly)
                        {
                            schema.ReadOnly = true;
                        }
                    }
                }

                if (schema.Properties != null)
                {
                    var excludedProperties = new List<string>();
                    var readonlyProperties = new List<string>();

                    context.Type.GetProperties().ToList().ForEach(property =>
                    {
                        var shouldExclude = false;
                        var shouldMarkAsReadOnly = false;

                        var propertyAttributes = property.GetCustomAttributes(true);

                        foreach (Attribute attribute in propertyAttributes)
                        {
                            if (attribute is SwaggerExtensionAttribute)
                            {
                                var swaggerAttribute = attribute as SwaggerExtensionAttribute;

                                if (swaggerAttribute.ExcludeFromSwagger)
                                {
                                    shouldExclude = true;
                                }

                                if (swaggerAttribute.MarkAsReadOnly)
                                {
                                    shouldMarkAsReadOnly = true;
                                }
                            }
                        }

                        if (shouldExclude)
                        {
                            excludedProperties.Add(ToCamelCase(property.Name));
                        }

                        if (shouldMarkAsReadOnly)
                        {
                            readonlyProperties.Add(ToCamelCase(property.Name));
                        }
                    });

                    foreach (var property in schema.Properties)
                    {
                        if (readonlyProperties.Contains(property.Key))
                        {
                            property.Value.ReadOnly = true;
                        }
                    }

                    foreach (var excludedProperty in excludedProperties)
                    {
                        if (schema.Properties.ContainsKey(excludedProperty))
                        {
                            schema.Properties.Remove(excludedProperty);
                        }
                    }
                }
            }
        }

        private static string ToCamelCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            else if (value.Length == 1)
            {
                return char.ToString(char.ToLowerInvariant(value[0]));
            }
            else
            {
                return char.ToLowerInvariant(value[0]) + value.Substring(1);
            }
        }
    }
}
