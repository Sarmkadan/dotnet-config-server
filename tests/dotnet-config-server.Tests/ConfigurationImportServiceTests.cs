using System.Text.Json;
using DotnetConfigServer.Services;
using DotnetConfigServer.Models;
using FluentAssertions;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetConfigServer.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ConfigurationImportService"/> that cover JSON, CSV, and environment variable import formats.
    /// Tests valid payloads, malformed payload rejection, duplicate key handling, and value preservation semantics.
    /// </summary>
    public sealed class ConfigurationImportServiceTests
    {
        private readonly ConfigurationImportService _service;
        private readonly Guid _testConfigurationId = Guid.NewGuid();

        public ConfigurationImportServiceTests()
        {
            var loggerMock = new Mock<ILogger<ConfigurationImportService>>();
            _service = new ConfigurationImportService(loggerMock.Object);
        }

        // ====================================================================
        // JSON Import Tests
        // ====================================================================

        [Fact]
        public async Task ImportFromJsonAsync_ValidJsonObject_ReturnsConfigurationKeys()
        {
            // Arrange
            var json = "{\r\n    \"database.host\": \"localhost\",\r\n    \"database.port\": \"5432\",\r\n    \"api.key\": \"secret123\"\r\n}";

            // Act
            var result = await _service.ImportFromJsonAsync(json, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            var keysByName = result.ToDictionary(k => k.Key);
            keysByName.Should().ContainKey("database.host");
            keysByName.Should().ContainKey("database.port");
            keysByName.Should().ContainKey("api.key");

            keysByName["database.host"].Value.Should().Be("localhost");
            keysByName["database.port"].Value.Should().Be("5432");
            keysByName["api.key"].Value.Should().Be("secret123");

            foreach (var key in result)
            {
                key.Id.Should().NotBe(Guid.Empty);
                key.ConfigurationId.Should().Be(_testConfigurationId);
                key.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
                key.IsActive.Should().BeTrue();
            }
        }

        [Fact]
        public async Task ImportFromJsonAsync_EmptyJsonObject_ReturnsEmptyList()
        {
            // Arrange
            var json = "{}";

            // Act
            var result = await _service.ImportFromJsonAsync(json, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportFromJsonAsync_MalformedJson_ThrowsInvalidOperationException()
        {
            // Arrange
            var malformedJson = "{ invalid";

            // Act & Assert
            var act = async () => await _service.ImportFromJsonAsync(malformedJson, _testConfigurationId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Invalid JSON format*");
        }

        [Fact]
        public async Task ImportFromJsonAsync_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string? nullJson = null;

            // Act & Assert
            var act = async () => await _service.ImportFromJsonAsync(nullJson!, _testConfigurationId);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ImportFromJsonAsync_JsonArray_ReturnsEmptyList()
        {
            // Arrange
            var jsonArray = "[\"item1\", \"item2\"]";

            // Act
            var result = await _service.ImportFromJsonAsync(jsonArray, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }


        // ====================================================================
        // CSV Import Tests
        // ====================================================================

        [Fact]
        public async Task ImportFromCsvAsync_ValidCsv_ReturnsConfigurationKeys()
        {
            // Arrange
            var csv = "Key,Value\n" +
                     "database.host,localhost\n" +
                     "database.port,5432\n" +
                     "api.key,secret123\n";

            // Act
            var result = await _service.ImportFromCsvAsync(csv, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            var keysByName = result.ToDictionary(k => k.Key);
            keysByName.Should().ContainKey("database.host");
            keysByName.Should().ContainKey("database.port");
            keysByName.Should().ContainKey("api.key");

            keysByName["database.host"].Value.Should().Be("localhost");
            keysByName["database.port"].Value.Should().Be("5432");
            keysByName["api.key"].Value.Should().Be("secret123");
        }

        [Fact]
        public async Task ImportFromCsvAsync_EmptyCsv_ReturnsEmptyList()
        {
            // Arrange
            var csv = string.Empty;

            // Act
            var result = await _service.ImportFromCsvAsync(csv, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportFromCsvAsync_SingleLineCsv_ReturnsEmptyList()
        {
            // Arrange
            var csv = "Key,Value";

            // Act
            var result = await _service.ImportFromCsvAsync(csv, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportFromCsvAsync_MissingRequiredColumns_ThrowsInvalidOperationException()
        {
            // Arrange
            var csv = "Column1,Column2\nvalue1,value2\n";

            // Act & Assert
            var act = async () => await _service.ImportFromCsvAsync(csv, _testConfigurationId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("CSV must contain 'Key' and 'Value' columns*");
        }

        [Fact]
        public async Task ImportFromCsvAsync_CsvWithWhitespace_TrimsValues()
        {
            // Arrange
            var csv = "Key,Value\n  database.host  ,  localhost  \n";

            // Act
            var result = await _service.ImportFromCsvAsync(csv, _testConfigurationId);

            // Assert
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("database.host");
            result[0].Value.Should().Be("localhost");
        }

        [Fact]
        public async Task ImportFromCsvAsync_CsvWithExtraColumns_IgnoresExtraColumns()
        {
            // Arrange
            var csv = "Key,Value,Description,Type\n" +
                     "database.host,localhost,Database host,string\n";

            // Act
            var result = await _service.ImportFromCsvAsync(csv, _testConfigurationId);

            // Assert
            result.Should().HaveCount(1);
            result[0].Key.Should().Be("database.host");
            result[0].Value.Should().Be("localhost");
        }

        // ====================================================================
        // Environment Variable Import Tests
        // ====================================================================

        [Fact]
        public async Task ImportFromEnvAsync_ValidEnvFormat_ReturnsConfigurationKeys()
        {
            // Arrange
            var envContent = "# Database configuration\n" +
                           "DATABASE_HOST=localhost\n" +
                           "DATABASE_PORT=5432\n" +
                           "API_KEY=secret123\n";

            // Act
            var result = await _service.ImportFromEnvAsync(envContent, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            var keysByName = result.ToDictionary(k => k.Key);
            keysByName.Should().ContainKey("DATABASE_HOST");
            keysByName.Should().ContainKey("DATABASE_PORT");
            keysByName.Should().ContainKey("API_KEY");

            keysByName["DATABASE_HOST"].Value.Should().Be("localhost");
            keysByName["DATABASE_PORT"].Value.Should().Be("5432");
            keysByName["API_KEY"].Value.Should().Be("secret123");
        }

        [Fact]
        public async Task ImportFromEnvAsync_EmptyEnvContent_ReturnsEmptyList()
        {
            // Arrange
            var envContent = string.Empty;

            // Act
            var result = await _service.ImportFromEnvAsync(envContent, _testConfigurationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportFromEnvAsync_EnvWithCommentsAndBlankLines_IgnoresCommentsAndBlanks()
        {
            // Arrange
            var envContent = "# This is a comment\n" +
                           "\n" +
                           "KEY1=value1\n" +
                           "# Another comment\n" +
                           "\n" +
                           "KEY2=value2\n";

            // Act
            var result = await _service.ImportFromEnvAsync(envContent, _testConfigurationId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainSingle(k => k.Key == "KEY1");
            result.Should().ContainSingle(k => k.Key == "KEY2");
        }

        [Fact]
        public async Task ImportFromEnvAsync_EnvWithoutEqualsSign_IgnoresInvalidLine()
        {
            // Arrange
            var envContent = "KEY1=value1\n" +
                           "INVALID_LINE\n" +
                           "KEY2=value2\n";

            // Act
            var result = await _service.ImportFromEnvAsync(envContent, _testConfigurationId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainSingle(k => k.Key == "KEY1");
            result.Should().ContainSingle(k => k.Key == "KEY2");
        }

        // ====================================================================
        // Validation Tests
        // ====================================================================

        [Fact]
        public async Task ValidateAsync_ValidJson_ReturnsValidResult()
        {
            // Arrange
            var validJson = "{\r\n    \"key1\": \"value1\",\r\n    \"key2\": \"value2\"\r\n}";

            // Act
            var result = await _service.ValidateAsync(validJson, "json");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_InvalidJson_ReturnsInvalidResultWithError()
        {
            // Arrange
            var invalidJson = "{ invalid";

            // Act
            var result = await _service.ValidateAsync(invalidJson, "json");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_ValidCsv_ReturnsValidResult()
        {
            // Arrange
            var validCsv = "Key,Value\nkey1,value1\n";

            // Act
            var result = await _service.ValidateAsync(validCsv, "csv");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_InvalidCsv_ReturnsInvalidResultWithError()
        {
            // Arrange
            var invalidCsv = "invalid";

            // Act
            var result = await _service.ValidateAsync(invalidCsv, "csv");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_ValidEnv_ReturnsValidResult()
        {
            // Arrange
            var validEnv = "KEY1=value1\nKEY2=value2\n";

            // Act
            var result = await _service.ValidateAsync(validEnv, "env");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_InvalidFormat_ReturnsInvalidResult()
        {
            // Arrange
            var data = "some data";

            // Act
            var result = await _service.ValidateAsync(data, "invalid_format");

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e == "Unknown format");
        }
    }
}