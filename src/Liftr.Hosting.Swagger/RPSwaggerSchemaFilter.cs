//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Utilities;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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
                if (context.Type.IsEnum)
                {
                    var enumExtension = new OpenApiObject();
                    enumExtension["modelAsString"] = new OpenApiBoolean(true);
                    schema.Extensions["x-ms-enum"] = enumExtension;
                }

                // Look for attributes in the type definition
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

                // Look for attributes in the properties of the type.
                // Mutability properties should only be applied at the property level,
                // not on the schema level.
                if (schema.Properties != null)
                {
                    var excludedProperties = new List<string>();
                    var readonlyProperties = new List<string>();
                    var mutabilityProperties = new Dictionary<string, OpenApiArray>();

                    context.Type.GetProperties().ToList().ForEach(property =>
                    {
                        var shouldExclude = false;
                        var shouldMarkAsReadOnly = false;
                        var mutabilityValue = MutabilityValues.None;

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

                                mutabilityValue = swaggerAttribute.MutabilityValues;
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

                        if (mutabilityValue != MutabilityValues.None)
                        {
                            var mutabilityList = GetMutabilityProperties(mutabilityValue);
                            mutabilityProperties.Add(ToCamelCase(property.Name), mutabilityList);
                        }
                    });

                    foreach (var property in schema.Properties)
                    {
                        if (readonlyProperties.Contains(property.Key))
                        {
                            property.Value.ReadOnly = true;
                        }

                        if (mutabilityProperties.ContainsKey(property.Key))
                        {
                            property.Value.Extensions.Add("x-ms-mutability", mutabilityProperties[property.Key]);
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

        private OpenApiArray GetMutabilityProperties(MutabilityValues values)
        {
            var returnValue = new OpenApiArray();

            if ((values & MutabilityValues.Create) == MutabilityValues.Create)
            {
                returnValue.Add(new OpenApiString("create"));
            }

            if ((values & MutabilityValues.Read) == MutabilityValues.Read)
            {
                returnValue.Add(new OpenApiString("read"));
            }

            if ((values & MutabilityValues.Update) == MutabilityValues.Update)
            {
                returnValue.Add(new OpenApiString("update"));
            }

            return returnValue;
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
