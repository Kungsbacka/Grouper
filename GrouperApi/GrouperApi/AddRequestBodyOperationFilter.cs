using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace GrouperApi
{
    internal sealed class AddRequestBodyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var httpMethod = context.ApiDescription.HttpMethod;

            // Only apply to POST methods
            if (httpMethod != null && httpMethod.Equals("POST", StringComparison.Ordinal))
            {
                // Check if the endpoint doesn't already have a request body defined
                operation.RequestBody ??= new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Example = new JsonObject(),
                                Description = "JSON request body"
                            }
                        }
                    },
                    Required = true,
                    Description = "JSON request body"
                };
            }
        }
    }
}
