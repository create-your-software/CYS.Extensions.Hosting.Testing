// <copyright file="HostApplicationBuilderFacade.cs" company="Create Your Software">
// Copyright (c) 2026 Create Your Software
// All rights reserved.
//
// This software is the proprietary property of Create Your Software.
// and is protected by copyright law and international treaties.
//
// Unauthorized use, reproduction, modification, distribution, or disclosure
// of this software, in whole or in part, is strictly prohibited without the
// express written permission of the copyright holder.
//
// Use without a valid license is strictly forbidden.
//
// Contact www.createyour.software for licensing information.
// </copyright>

namespace CYS.Extensions.Hosting.Testing
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.Metrics;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Adapts the host configuration exposed through an <see cref="IHostBuilder"/> to the
    /// <see cref="IHostApplicationBuilder"/> interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The façade is created while the underlying host builder executes its
    /// service-configuration callbacks. It exposes the current <see cref="HostBuilderContext"/>,
    /// the application <see cref="IServiceCollection"/>, and the host builder's shared properties
    /// through the corresponding <see cref="IHostApplicationBuilder"/> members.
    /// </para>
    /// <para>
    /// Service, logging, and metrics configuration modifies the application
    /// service collection directly. Container configuration is forwarded to the
    /// underlying <see cref="IHostBuilder"/>.
    /// </para>
    /// </remarks>
    internal sealed class HostApplicationBuilderFacade : IHostApplicationBuilder
    {
        private readonly IHostBuilder hostBuilder;

        /// <summary>Initializes a new instance of the <see cref="HostApplicationBuilderFacade"/> class.
        /// </summary>
        /// <param name="hostBuilder">
        /// The underlying host builder to which container configuration is forwarded
        /// and from which shared host-building properties are obtained.
        /// </param>
        /// <param name="context">
        /// The current host-building context containing the application configuration
        /// and hosting environment exposed by the façade.
        /// </param>
        /// <param name="services">
        /// The application service collection exposed through the service, logging,
        /// and metrics builder properties of the façade.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostBuilder"/>,
        /// <paramref name="context"/>, or <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public HostApplicationBuilderFacade(
                                            IHostBuilder hostBuilder,
                                            HostBuilderContext context,
                                            IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(services);

            this.hostBuilder = hostBuilder;

            this.Configuration = (IConfigurationManager)context.Configuration;
            this.Environment = context.HostingEnvironment;
            this.Properties = this.hostBuilder.Properties;

            this.Services = services;
            this.Logging = new LoggingBuilderFacade(services);
            this.Metrics = new MetricsBuilderFacade(services);
        }

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; }

        /// <inheritdoc />
        public IConfigurationManager Configuration { get; }

        /// <inheritdoc />
        public IHostEnvironment Environment { get; }

        /// <inheritdoc />
        public ILoggingBuilder Logging { get; }

        /// <inheritdoc />
        public IMetricsBuilder Metrics { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }

        // This method is only used when the DI Container is replaced with a different container.

        /// <inheritdoc />
        public void ConfigureContainer<TContainerBuilder>(
            IServiceProviderFactory<TContainerBuilder> factory,
            Action<TContainerBuilder>? configure = null)
            where TContainerBuilder : notnull
        {
            this.hostBuilder.UseServiceProviderFactory(factory);

            if (configure is not null)
            {
                this.hostBuilder.ConfigureContainer<TContainerBuilder>(
                    (_, containerBuilder) => configure(containerBuilder));
            }
        }
    }
}
