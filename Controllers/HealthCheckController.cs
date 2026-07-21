#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for health and readiness checks
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthCheckController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.GetHealthReportAsync();
        return Ok(report);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReady()
    {
        var isReady = await _healthCheckService.IsReadyAsync();
        return Ok(new { status = isReady ? "ready" : "not_ready" });
    }

    [HttpGet("alive")]
    public async Task<IActionResult> GetAlive()
    {
        var isAlive = await _healthCheckService.IsAliveAsync();
        return Ok(new { status = isAlive ? "alive" : "dead" });
    }
}