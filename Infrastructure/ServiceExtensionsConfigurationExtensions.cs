using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetConfigServer.Infrastructure
{
	/// <summary>
	/// Provides extension methods for <see cref="ServiceExtensionsConfiguration"/> to enable fluent configuration
	/// and common operations on service extension configurations.
	/// </summary>
	public static class ServiceExtensionsConfigurationExtensions
	{
		/// <summary>
		/// Determines whether the configuration contains any data services.
		/// </summary>
		/// <param name="configuration">The service extension configuration to check.</param>
		/// <returns><see langword="true"/> if the configuration contains data services; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static bool HasDataServices(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			return configuration.DataServices is not null && configuration.DataServices.Length > 0;
		}

		/// <summary>
		/// Determines whether the configuration contains any business services.
		/// </summary>
		/// <param name="configuration">The service extension configuration to check.</param>
		/// <returns><see langword="true"/> if the configuration contains business services; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static bool HasBusinessServices(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			return configuration.BusinessServices is not null && configuration.BusinessServices.Length > 0;
		}

		/// <summary>
		/// Gets all configured service types as a read-only collection.
		/// </summary>
		/// <param name="configuration">The service extension configuration.</param>
		/// <returns>A read-only collection of all configured service types.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static IReadOnlyList<string> GetAllServiceTypes(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);

			return configuration switch
			{
				null => throw new ArgumentNullException(nameof(configuration)),
				{ DataServices: null, BusinessServices: null, WebhookClient: null } => Array.Empty<string>(),
				var c => GetAllServiceTypesCore(c),
			};
		}

		private static IReadOnlyList<string> GetAllServiceTypesCore(ServiceExtensionsConfiguration configuration)
		{
			var services = new List<string>();

			if (configuration.DataServices is not null)
			{
				services.AddRange(configuration.DataServices);
			}

			if (configuration.BusinessServices is not null)
			{
				services.AddRange(configuration.BusinessServices);
			}

			if (configuration.WebhookClient is not null)
			{
				services.AddRange(configuration.WebhookClient);
			}

			return services.AsReadOnly();
		}

		/// <summary>
		/// Determines whether the configuration has any Swagger configuration.
		/// </summary>
		/// <param name="configuration">The service extension configuration to check.</param>
		/// <returns><see langword="true"/> if the configuration contains Swagger configuration; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static bool HasSwaggerConfiguration(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			return configuration.SwaggerConfiguration is not null && configuration.SwaggerConfiguration.Length > 0;
		}

		/// <summary>
		/// Determines whether the configuration has any database initialization scripts.
		/// </summary>
		/// <param name="configuration">The service extension configuration to check.</param>
		/// <returns><see langword="true"/> if the configuration contains database initialization scripts; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static bool HasDatabaseInitialization(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			return configuration.DatabaseInitialization is not null && configuration.DatabaseInitialization.Length > 0;
		}

		/// <summary>
		/// Gets the count of all configured services across all categories.
		/// </summary>
		/// <param name="configuration">The service extension configuration.</param>
		/// <returns>The total number of configured services.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static int GetServiceCount(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);

			return (configuration.DataServices?.Length ?? 0)
			+ (configuration.BusinessServices?.Length ?? 0)
			+ (configuration.WebhookClient?.Length ?? 0);
		}

		/// <summary>
		/// Determines whether the configuration contains any services at all.
		/// </summary>
		/// <param name="configuration">The service extension configuration to check.</param>
		/// <returns><see langword="true"/> if the configuration contains any services; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
		public static bool HasAnyServices(this ServiceExtensionsConfiguration? configuration)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			return configuration.GetServiceCount() > 0;
		}

		/// <summary>
		/// Creates a new <see cref="ServiceExtensionsConfiguration"/> with the specified data services added.
		/// </summary>
		/// <param name="configuration">The base configuration.</param>
		/// <param name="dataServices">The data services to add.</param>
		/// <returns>A new configuration with the data services added.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> or <paramref name="dataServices"/> is <see langword="null"/>.</exception>
		public static ServiceExtensionsConfiguration WithAddedDataServices(this ServiceExtensionsConfiguration? configuration, IEnumerable<string> dataServices)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			ArgumentNullException.ThrowIfNull(dataServices);

			var newDataServices = configuration.DataServices?.ToList() ?? new List<string>();
			newDataServices.AddRange(dataServices);

			return new ServiceExtensionsConfiguration
			{
				DataServices = newDataServices.Distinct().ToArray(),
				BusinessServices = configuration.BusinessServices,
				WebhookClient = configuration.WebhookClient,
				SwaggerConfiguration = configuration.SwaggerConfiguration,
				DatabaseInitialization = configuration.DatabaseInitialization
			};
		}

		/// <summary>
		/// Creates a new <see cref="ServiceExtensionsConfiguration"/> with the specified business services added.
		/// </summary>
		/// <param name="configuration">The base configuration.</param>
		/// <param name="businessServices">The business services to add.</param>
		/// <returns>A new configuration with the business services added.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="configuration"/> or <paramref name="businessServices"/> is <see langword="null"/>.</exception>
		public static ServiceExtensionsConfiguration WithAddedBusinessServices(this ServiceExtensionsConfiguration? configuration, IEnumerable<string> businessServices)
		{
			ArgumentNullException.ThrowIfNull(configuration);
			ArgumentNullException.ThrowIfNull(businessServices);

			var newBusinessServices = configuration.BusinessServices?.ToList() ?? new List<string>();
			newBusinessServices.AddRange(businessServices);

			return new ServiceExtensionsConfiguration
			{
				DataServices = configuration.DataServices,
				BusinessServices = newBusinessServices.Distinct().ToArray(),
				WebhookClient = configuration.WebhookClient,
				SwaggerConfiguration = configuration.SwaggerConfiguration,
				DatabaseInitialization = configuration.DatabaseInitialization
			};
		}
	}
}
