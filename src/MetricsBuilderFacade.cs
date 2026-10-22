// <copyright file="MetricsBuilderFacade.cs" company="Create Your Software">
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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.Metrics;
    using System;

    /// <summary>
    /// Adapts an <see cref="IServiceCollection"/> for use as an <see cref="IMetricsBuilder"/>.
    /// </summary>
    internal sealed class MetricsBuilderFacade : IMetricsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsBuilderFacade"/> class.
        /// </summary>
        /// <param name="services">
        /// The application service collection used to register metrics services.
        /// </param>
        public MetricsBuilderFacade(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            this.Services = services;
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
