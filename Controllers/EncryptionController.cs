#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for encryption key rotation and on-demand re-encryption of stored
/// configuration secrets.
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/encryption")]
[Produces("application/json")]
public sealed class EncryptionController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly ILogger<EncryptionController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EncryptionController"/>.
    /// </summary>
    /// <param name="encryptionService">Service performing encryption, key rotation, and re-encryption.</param>
    /// <param name="keyRepository">Repository used to persist re-encrypted configuration values.</param>
    /// <param name="logger">Logger used to report request outcomes.</param>
    public EncryptionController(
        IEncryptionService encryptionService,
        IConfigurationKeyRepository keyRepository,
        ILogger<EncryptionController> logger)
    {
        _encryptionService = encryptionService;
        _keyRepository = keyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Marks an encryption key as rotated (no longer primary, but still active for decryption)
    /// as the first step of a key rotation.
    /// </summary>
    /// <param name="oldKeyId">The identifier of the key being rotated out.</param>
    /// <param name="userId">The identifier of the user performing the rotation.</param>
    [HttpPost("keys/{oldKeyId}/rotate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateKey([FromRoute] string oldKeyId, [FromQuery] string userId)
    {
        try
        {
            await _encryptionService.RotateKeyAsync(oldKeyId, userId);
            return NoContent();
        }
        catch (ConfigurationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating encryption key {KeyId}", oldKeyId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Re-encrypts every encrypted value belonging to the configuration with its current
    /// primary encryption key, migrating any values still protected by an older key version.
    /// </summary>
    /// <param name="configurationId">The configuration whose values should be migrated.</param>
    /// <param name="userId">The identifier of the user requesting the migration.</param>
    [HttpPost("re-encrypt")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReEncryptConfiguration([FromRoute] Guid configurationId, [FromQuery] string userId)
    {
        try
        {
            var keys = await _keyRepository.GetByConfigurationAsync(configurationId);
            var encryptedKeys = keys.Where(k => k.IsEncrypted).ToList();

            await _encryptionService.ReEncryptConfigurationAsync(configurationId, encryptedKeys, userId);

            foreach (var key in encryptedKeys)
                await _keyRepository.UpdateAsync(key);

            return Ok(new { configurationId, migratedCount = encryptedKeys.Count });
        }
        catch (ConfigurationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-encrypting configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
