#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for health and readiness checks
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy" });
    }

    [HttpGet("ready")]
    public IActionResult GetReady()
    {
        return Ok(new { status = "ready" });
    }
}
