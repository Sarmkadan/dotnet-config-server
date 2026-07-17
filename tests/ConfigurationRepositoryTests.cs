using Xunit;
using FluentAssertions;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace dotnet_config_server.tests
{
/// <summary>
/// Unit tests for <see cref="ConfigurationRepository"/> class
/// </summary>
    public class ConfigurationRepositoryTests
    {
        [Fact]
        /// <summary>
        /// Tests that <see cref="ConfigurationRepository.GetByApplicationIdAsync(Guid)"/> returns configurations
        /// when the application ID is valid and configurations exist for that application
        /// </summary>
        public async Task GetByApplicationIdAsync_ReturnsConfigurations_WhenApplicationIdIsValid()
        {
            // Arrange
            var mockDbContext = new Mock<IConfigurationRepository>();
            var applicationId = Guid.NewGuid();
            var configurations = new List<Models.Configuration>
            {
                new Models.Configuration { Id = Guid.NewGuid(), ApplicationId = applicationId },
                new Models.Configuration { Id = Guid.NewGuid(), ApplicationId = applicationId }
            };

            mockDbContext.Setup(repo => repo.GetByApplicationIdAsync(applicationId)).ReturnsAsync(configurations);

            var repository = new ConfigurationRepository(mockDbContext.Object);

            // Act
            var result = await repository.GetByApplicationIdAsync(applicationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(configurations[0]);
            result.Should().Contain(configurations[1]);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="ConfigurationRepository.GetByApplicationIdAsync(Guid)"/> throws
        /// <see cref="ArgumentNullException"/> when the application ID is null
        /// </summary>
        public async Task GetByApplicationIdAsync_ThrowsArgumentNullException_WhenApplicationIdIsNull()
        {
            // Arrange
            var mockDbContext = new Mock<IConfigurationRepository>();
            var repository = new ConfigurationRepository(mockDbContext.Object);

            // Act and Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetByApplicationIdAsync(null));
        }
    }
}
