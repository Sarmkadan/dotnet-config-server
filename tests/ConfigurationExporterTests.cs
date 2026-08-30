using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using DotnetConfigServer.Formatters;
using DotnetConfigServer.Models;

namespace dotnet_config_server.tests
{
    /// <summary>
    /// Unit tests for <see cref="ConfigurationExporter"/> class
    /// </summary>
    public class ConfigurationExporterTests
    {
        // Helper to wrap an IEnumerable as IAsyncEnumerable
        private static IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
        {
            return ConfigurationExporter.ToAsyncEnumerable(source);
        }

        [Fact]
        public async Task WriteAsJsonAsync_ReturnsEmptyJsonArray_WhenNoConfigurations()
        {
            // Arrange
            var configurations = new List<Configuration>();
            await using var stream = new MemoryStream();

            // Act
            await ConfigurationExporter.WriteAsJsonAsync(stream, ToAsyncEnumerable(configurations));

            // Assert
            stream.Position = 0;
            using var jsonDoc = await JsonDocument.ParseAsync(stream);
            jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            jsonDoc.RootElement.GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task WriteAsJsonAsync_ReturnsJsonArrayWithOneConfiguration_WhenSingleConfiguration()
        {
            // Arrange
            var config = new Configuration
            {
                Id = Guid.NewGuid(),
                ApplicationId = Guid.NewGuid(),
                Name = "Test Config",
                Description = "Test Description",
                Environment = Models.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = "2026-01-01T00:00:00Z",
                UpdatedAt = "2026-01-01T00:00:00Z",
                CreatedBy = "tester"
            };
            var configurations = new List<Configuration> { config };
            await using var stream = new MemoryStream();

            // Act
            await ConfigurationExporter.WriteAsJsonAsync(stream, ToAsyncEnumerable(configurations));

            // Assert
            stream.Position = 0;
            using var jsonDoc = await JsonDocument.ParseAsync(stream);
            jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            jsonDoc.RootElement.GetArrayLength().Should().Be(1);

            var configElement = jsonDoc.RootElement.EnumerateArray().First();
            configElement.GetProperty("id").GetString().Should().Be(config.Id.ToString());
            configElement.GetProperty("applicationId").GetString().Should().Be(config.ApplicationId.ToString());
            configElement.GetProperty("name").GetString().Should().Be(config.Name);
            configElement.GetProperty("description").GetString().Should().Be(config.Description);
            configElement.GetProperty("environment").GetString().Should().Be(config.Environment.ToString());
            configElement.GetProperty("isActive").GetBoolean().Should().Be(config.IsActive);
            configElement.GetProperty("isEncrypted").GetBoolean().Should().Be(config.IsEncrypted);
            configElement.GetProperty("createdAt").GetString().Should().Be(config.CreatedAt);
            configElement.GetProperty("updatedAt").GetString().Should().Be(config.UpdatedAt);
            configElement.GetProperty("createdBy").GetString().Should().Be(config.CreatedBy);
        }

        [Fact]
        public async Task WriteAsJsonAsync_ReturnsJsonArrayWithMultipleConfigurations_WhenManyConfigurations()
        {
            // Arrange
            var configs = new List<Configuration>
            {
                new Configuration
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = Guid.NewGuid(),
                    Name = "Config 1",
                    Description = "First config",
                    Environment = Models.Environment.Development,
                    IsActive = true,
                    IsEncrypted = false,
                    CreatedAt = "2026-01-01T00:00:00Z",
                    UpdatedAt = "2026-01-01T00:00:00Z",
                    CreatedBy = "tester"
                },
                new Configuration
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = Guid.NewGuid(),
                    Name = "Config 2",
                    Description = "Second config",
                    Environment = Models.Environment.Production,
                    IsActive = false,
                    IsEncrypted = true,
                    CreatedAt = "2026-01-02T00:00:00Z",
                    UpdatedAt = "2026-01-02T00:00:00Z",
                    CreatedBy = "tester2"
                }
            };
            await using var stream = new MemoryStream();

            // Act
            await ConfigurationExporter.WriteAsJsonAsync(stream, ToAsyncEnumerable(configs));

            // Assert
            stream.Position = 0;
            using var jsonDoc = await JsonDocument.ParseAsync(stream);
            jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
            jsonDoc.RootElement.GetArrayLength().Should().Be(2);

            var enumerator = jsonDoc.RootElement.EnumerateArray();
            var first = enumerator.Current;
            enumerator.MoveNext();
            var second = enumerator.Current;

            first.GetProperty("name").GetString().Should().Be("Config 1");
            first.GetProperty("environment").GetString().Should().Be("Development");
            first.GetProperty("isActive").GetBoolean().Should().BeTrue();

            second.GetProperty("name").GetString().Should().Be("Config 2");
            second.GetProperty("environment").GetString().Should().Be("Production");
            second.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task WriteAsJsonAsync_ProducesNonIndentedOutput_WhenPrettyFalse()
        {
            // Arrange
            var config = new Configuration
            {
                Id = Guid.NewGuid(),
                ApplicationId = Guid.NewGuid(),
                Name = "Test",
                Description = "Test",
                Environment = Models.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = "2026-01-01T00:00:00Z",
                UpdatedAt = "2026-01-01T00:00:00Z",
                CreatedBy = "tester"
            };
            var configurations = new List<Configuration> { config };
            await using var stream = new MemoryStream();

            // Act
            await ConfigurationExporter.WriteAsJsonAsync(stream, ToAsyncEnumerable(configurations), pretty: false);

            // Assert
            stream.Position = 0;
            var jsonString = await new StreamReader(stream).ReadToEndAsync();
            // Should not contain indentation (newline followed by two spaces)
            jsonString.Should().NotContain("\n  ");
            // Should still be valid JSON
            using var jsonDoc = JsonDocument.Parse(jsonString);
            jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        }

        [Fact]
        public async Task WriteAsJsonAsync_ThrowsArgumentNullException_WhenDestinationIsNull()
        {
            // Arrange
            var configurations = new List<Configuration>();
            IAsyncEnumerable<Configuration> asyncConfigs = ToAsyncEnumerable(configurations);

            // Act & Assert
            await Func<Task>(async () =>
                await ConfigurationExporter.WriteAsJsonAsync(null!, asyncConfigs))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WriteAsJsonAsync_ThrowsArgumentNullException_WhenConfigurationsIsNull()
        {
            // Arrange
            await using var stream = new MemoryStream();
            IAsyncEnumerable<Configuration> asyncConfigs = null!;

            // Act & Assert
            await Func<Task>(async () =>
                await ConfigurationExporter.WriteAsJsonAsync(stream, asyncConfigs))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WriteAsJsonAsync_ThrowsOperationCanceledException_WhenCancellationTokenIsCanceled()
        {
            // Arrange
            var configurations = new List<Configuration> { new Configuration { Id = Guid.NewGuid() } };
            await using var stream = new MemoryStream();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-canceled token

            // Act & Assert
            await Func<Task>(async () =>
                await ConfigurationExporter.WriteAsJsonAsync(stream, ToAsyncEnumerable(configurations), cancellationToken: cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void ExportAsCsv_EscapesSpecialCharactersCorrectly()
        {
            // Arrange
            var config = new Configuration
            {
                Id = Guid.NewGuid(),
                ApplicationId = Guid.NewGuid(),
                Name = "Test, \"Quote\" and newline\n",
                Description = "Description with, comma and \"quote\"",
                Environment = Models.Environment.Development,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = "2026-01-01T00:00:00Z",
                UpdatedAt = "2026-01-01T00:00:00Z",
                CreatedBy = "tester"
            };
            var configurations = new List<Configuration> { config };

            // Act
            var csv = ConfigurationExporter.ExportAsCsv(configurations);

            // Assert
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.Should().Be(2); // header + one data line
            var dataLine = lines[1];
            // The CSV should have the values properly escaped
            // Name: contains comma, quote, newline -> should be quoted and internal quotes doubled
            // Description: contains comma and quote -> should be quoted and internal quotes doubled
            dataLine.Should().Contain("\"Test, \"\"Quote\"\" and newline\n\"");
            dataLine.Should().Contain("\"Description with, comma and \"\"quote\"\"\"");
        }
    }
}