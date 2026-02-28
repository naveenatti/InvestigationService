using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Investigation.API.Swagger.Filters
{
    /// <summary>
    /// Adds a traceId response header definition to every operation.
    /// </summary>
    public class AddTraceIdHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses == null) return;
            foreach (var resp in operation.Responses.Values)
            {
                resp.Headers ??= new Dictionary<string, OpenApiHeader>();
                resp.Headers["traceId"] = new OpenApiHeader
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
    public class AddResponseDescriptionFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses.ContainsKey("200"))
                operation.Responses["200"].Description = "Investigation result";
            if (operation.Responses.ContainsKey("400"))
                operation.Responses["400"].Description = "Validation error";
            if (operation.Responses.ContainsKey("500"))
                operation.Responses["500"].Description = "Unexpected server error";
        }
    }
}
