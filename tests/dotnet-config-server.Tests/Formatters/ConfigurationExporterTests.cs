#nullable enable

using DotnetConfigServer.Formatters;
using DotnetConfigServer.Models;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace DotnetConfigServer.Tests.Formatters;

/// <summary>
/// Unit tests for <see cref="ConfigurationExporter"/> covering its JSON, CSV, XML,
/// environment variable (.env), and YAML export formats for configurations and configuration keys.
/// </summary>
public sealed class ConfigurationExporterTests
{
    /// <summary>
    /// Verifies that exporting a fully populated configuration produces non-empty JSON that
    /// contains the configuration name ("TestConfig"), the creator ("testuser"), an
    /// "environment" property, and is wrapped in a top-level JSON array.
    /// </summary>
    [Fact]
    public void ExportAsJson_WithConfigurations_ReturnsValidJson()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                Description = "Test configuration",
                Environment = Common.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "testuser"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsJson(configs);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TestConfig");
        result.Should().Contain("testuser");
        result.Should().Contain("environment");
        result.Should().StartWith("[");
        result.Should().EndWith("]");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration collection serializes to the literal
    /// empty JSON array string "[]".
    /// </summary>
    [Fact]
    public void ExportAsJson_WithEmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        var configs = Array.Empty<Configuration>();

        // Act
        var result = ConfigurationExporter.ExportAsJson(configs);

        // Assert
        result.Should().Be("[]");
    }

    /// <summary>
    /// Verifies that enabling pretty printing indents the JSON output so the result spans
    /// multiple lines (contains newline characters).
    /// </summary>
    [Fact]
    public void ExportAsJson_PrettyPrintEnabled_ReturnsFormattedJson()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                CreatedBy = "user"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsJson(configs, pretty: true);

        // Assert
        result.Should().Contain("{\n");
        result.Should().Contain("\n");
    }

    /// <summary>
    /// Verifies that disabling pretty printing produces compact single-line JSON with no
    /// newline characters while retaining the configuration name and creator values.
    /// </summary>
    [Fact]
    public void ExportAsJson_PrettyPrintDisabled_ReturnsCompactJson()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                CreatedBy = "user"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsJson(configs, pretty: false);

        // Assert
        result.Should().NotContain("\n");
        result.Should().Contain("TestConfig");
        result.Should().Contain("user");
        result.Should().StartWith("[");
        result.Should().EndWith("]");
    }

    /// <summary>
    /// Verifies that exporting a configuration key produces non-empty JSON containing the
    /// key name ("ConnectionString") and its value ("Server=localhost").
    /// </summary>
    [Fact]
    public void ExportKeysAsJson_WithKeys_ReturnsValidJson()
    {
        // Arrange
        var keys = new[]
        {
            new ConfigurationKey
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "ConnectionString",
                Value = "Server=localhost",
                Description = "Database connection",
                IsEncrypted = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var result = ConfigurationExporter.ExportKeysAsJson(keys);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("ConnectionString");
        result.Should().Contain("Server=localhost");
    }

    /// <summary>
    /// Verifies that exporting configurations as CSV emits the expected column header row
    /// (Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy)
    /// followed by data rows containing the configuration name and creator.
    /// </summary>
    [Fact]
    public void ExportAsCsv_WithConfigurations_ReturnsCsvWithHeader()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                Description = "Test description",
                Environment = Common.Environment.Production,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "admin"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsCsv(configs);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy");
        result.Should().Contain("TestConfig");
        result.Should().Contain("admin");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration collection as CSV still emits the
    /// column header row followed by a newline and no data rows.
    /// </summary>
    [Fact]
    public void ExportAsCsv_WithEmptyCollection_ReturnsOnlyHeader()
    {
        // Arrange
        var configs = Array.Empty<Configuration>();

        // Act
        var result = ConfigurationExporter.ExportAsCsv(configs);

        // Assert
        result.Should().StartWith("Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy");
        result.Should().Contain("\n");
    }

    /// <summary>
    /// Verifies that a configuration name containing a comma ("Test,Config") is enclosed in
    /// double quotes in the CSV output so embedded commas do not break the field structure.
    /// </summary>
    [Fact]
    public void ExportAsCsv_WithCommaInName_EscapesProperly()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Test,Config",
                Description = "Description with quotes",
                Environment = Common.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "user"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsCsv(configs);

        // Assert
        result.Should().Contain("\"Test,Config\"");
        result.Should().Contain("quotes");
    }

    /// <summary>
    /// Verifies that exporting configuration keys as CSV emits the expected column header row
    /// (Id,ConfigurationId,Key,Value,Description,IsEncrypted,IsActive,CreatedAt)
    /// followed by data rows containing the key name and value.
    /// </summary>
    [Fact]
    public void ExportKeysAsCsv_WithKeys_ReturnsCsvWithHeader()
    {
        // Arrange
        var keys = new[]
        {
            new ConfigurationKey
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "ApiKey",
                Value = "secret123",
                Description = "API key",
                IsEncrypted = true,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var result = ConfigurationExporter.ExportKeysAsCsv(keys);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("Id,ConfigurationId,Key,Value,Description,IsEncrypted,IsActive,CreatedAt");
        result.Should().Contain("ApiKey");
        result.Should().Contain("secret123");
    }

    /// <summary>
    /// Verifies that exporting configurations as XML wraps the data in a &lt;Configurations&gt;
    /// root element with child elements carrying the configuration name and creator.
    /// </summary>
    [Fact]
    public void ExportAsXml_WithConfigurations_ReturnsValidXml()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                Description = "Test description",
                Environment = Common.Environment.Staging,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "devuser"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsXml(configs);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<Configurations>");
        result.Should().Contain("</Configurations>");
        result.Should().Contain("<Name>TestConfig</Name>");
        result.Should().Contain("<CreatedBy>devuser</CreatedBy>");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration collection as XML serializes to the
    /// self-closing empty root element "&lt;Configurations /&gt;".
    /// </summary>
    [Fact]
    public void ExportAsXml_WithEmptyCollection_ReturnsEmptyRootElement()
    {
        // Arrange
        var configs = Array.Empty<Configuration>();

        // Act
        var result = ConfigurationExporter.ExportAsXml(configs);

        // Assert
        result.Should().Be("<Configurations />");
    }

    /// <summary>
    /// Verifies that exporting configuration keys as XML wraps the data in a
    /// &lt;ConfigurationKeys&gt; root element with &lt;KeyName&gt; and &lt;Value&gt;
    /// child elements holding the key name and value.
    /// </summary>
    [Fact]
    public void ExportKeysAsXml_WithKeys_ReturnsValidXml()
    {
        // Arrange
        var keys = new[]
        {
            new ConfigurationKey
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "DatabaseUrl",
                Value = "postgres://localhost:5432/mydb",
                Description = "Database URL",
                IsEncrypted = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var result = ConfigurationExporter.ExportKeysAsXml(keys);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<ConfigurationKeys>");
        result.Should().Contain("</ConfigurationKeys>");
        result.Should().Contain("<KeyName>DatabaseUrl</KeyName>");
        result.Should().Contain("<Value>postgres://localhost:5432/mydb</Value>");
    }

    /// <summary>
    /// Verifies that exporting active configuration keys in .env format produces one
    /// KEY=VALUE pair per line containing API_KEY/secret123 and DATABASE_URL/postgres://localhost/db.
    /// </summary>
    [Fact]
    public void ExportAsEnvFormat_WithKeys_ReturnsKeyValuePairs()
    {
        // Arrange
        var keys = new[]
        {
            new ConfigurationKey
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "API_KEY",
                Value = "secret123",
                IsActive = true
            },
            new ConfigurationKey
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "DATABASE_URL",
                Value = "postgres://localhost/db",
                IsActive = true
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsEnvFormat(keys);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("API_KEY");
        result.Should().Contain("secret123");
        result.Should().Contain("DATABASE_URL");
        result.Should().Contain("postgres://localhost/db");
        result.Should().Contain("\n");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration key collection in .env format
    /// returns an empty string.
    /// </summary>
    [Fact]
    public void ExportAsEnvFormat_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var keys = Array.Empty<ConfigurationKey>();

        // Act
        var result = ConfigurationExporter.ExportAsEnvFormat(keys);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that exporting configurations as YAML produces a sequence item containing the
    /// configuration Id, Name, and boolean IsActive/IsEncrypted properties rendered as true/false.
    /// </summary>
    [Fact]
    public void ExportAsYaml_WithConfigurations_ReturnsValidYaml()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "TestConfig",
                Description = "Test description",
                Environment = Common.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "testuser"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsYaml(configs);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("- Id: 11111111-1111-1111-1111-111111111111");
        result.Should().Contain("Name: TestConfig");
        result.Should().Contain("IsActive: true");
        result.Should().Contain("IsEncrypted: false");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration collection as YAML returns an empty string.
    /// </summary>
    [Fact]
    public void ExportAsYaml_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var configs = Array.Empty<Configuration>();

        // Act
        var result = ConfigurationExporter.ExportAsYaml(configs);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that exporting configuration keys as YAML produces a sequence item containing
    /// the key Id, Key name, and Value fields.
    /// </summary>
    [Fact]
    public void ExportKeysAsYaml_WithKeys_ReturnsValidYaml()
    {
        // Arrange
        var keys = new[]
        {
            new ConfigurationKey
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ConfigurationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "ConnectionString",
                Value = "Server=localhost;Database=test",
                Description = "Database connection string",
                IsEncrypted = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        var result = ConfigurationExporter.ExportKeysAsYaml(keys);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("- Id: 33333333-3333-3333-3333-333333333333");
        result.Should().Contain("Key: ConnectionString");
        result.Should().Contain("Value: Server=localhost;Database=test");
    }

    /// <summary>
    /// Verifies that exporting an empty configuration key collection as YAML returns an empty string.
    /// </summary>
    [Fact]
    public void ExportKeysAsYaml_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var keys = Array.Empty<ConfigurationKey>();

        // Act
        var result = ConfigurationExporter.ExportKeysAsYaml(keys);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a configuration name containing colons ("Config:With:Colons") is preserved
    /// verbatim in the YAML output instead of being altered or dropped.
    /// </summary>
    [Fact]
    public void ExportAsYaml_WithSpecialCharactersInName_EscapesProperly()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Config:With:Colons",
                Description = "Description with hash and dash",
                Environment = Common.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                CreatedBy = "user"
            }
        };

        // Act
        var result = ConfigurationExporter.ExportAsYaml(configs);

        // Assert
        result.Should().Contain("Name:");
        result.Should().Contain("Config:With:Colons");
        result.Should().Contain("hash");
    }
}
