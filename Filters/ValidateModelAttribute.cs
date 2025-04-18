#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotnetConfigServer.Filters;

/// <summary>
/// Action filter that automatically validates model state before action execution.
/// Returns 400 Bad Request with detailed error messages if validation fails.
/// </summary>
public sealed class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
                );

            context.Result = new BadRequestObjectResult(new
            {
                error = "Model validation failed",
                details = errors
            });
        }
    }
}
