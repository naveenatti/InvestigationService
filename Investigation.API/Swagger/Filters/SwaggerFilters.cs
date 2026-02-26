using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Investigation.API.Swagger.Filters
{
    /// <summary>
    /// Adds a traceId response header to all 200 responses.
    /// </summary>
    public class AddTraceIdHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses.TryGetValue("200", out var response))
            {
                response.Headers ??= new Dictionary<string, OpenApiHeader>();
                response.Headers["traceId"] = new OpenApiHeader
                {
                    Description = "Correlation trace identifier",
                    Schema = new OpenApiSchema { Type = "string" }
                };
            }
        }
    }

    /// <summary>
    /// Adds default descriptions to common response codes.
    /// </summary>
   
}
