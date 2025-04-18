using Xunit;
using FluentAssertions;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace dotnet_config_server.tests
{
    public class ConfigurationRepositoryTests
    {
        [Fact]
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
