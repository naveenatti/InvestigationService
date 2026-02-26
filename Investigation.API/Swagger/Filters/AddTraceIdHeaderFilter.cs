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
            if (operation.Responses != null)
            {
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
    }
}