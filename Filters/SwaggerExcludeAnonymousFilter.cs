using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace Konnect_4New.Filters
{
    public class SwaggerExcludeAnonymousFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if AllowAnonymous is on method or declaring type (controller)
            var hasAllowAnonymous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any()
                || context.MethodInfo.DeclaringType
                    .GetCustomAttributes(true)
                    .OfType<AllowAnonymousAttribute>()
                    .Any();

            if (hasAllowAnonymous)
            {
                operation.Security.Clear(); // Remove lock icon
            }
        }
    }
}
