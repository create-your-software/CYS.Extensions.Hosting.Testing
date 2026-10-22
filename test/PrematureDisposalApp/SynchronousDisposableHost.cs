// <copyright file="SynchronousDisposableHost.cs" company="Create Your Software">
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

namespace CYS.PrematureDisposalApp
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.Metrics;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    public sealed class SynchronousDisposableHost : IHost, IHostApplicationBuilder
    {
        private static readonly DiagnosticListener HostingDiagnosticListener = new("Microsoft.Extensions.Hosting");

        private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SynchronousDisposableHost()
        {
            SynchronousDisposableHost.HostingDiagnosticListener.Write("HostBuilding", this);

            SynchronousDisposableHost.HostingDiagnosticListener.Write("HostBuilt", this);
        }

        public bool IsDisposed { get; private set; }

        public IDictionary<object, object> Properties => throw new NotImplementedException();

        public IConfigurationManager Configuration => throw new NotImplementedException();

        public IHostEnvironment Environment => throw new NotImplementedException();

        public ILoggingBuilder Logging => throw new NotImplementedException();

        public IMetricsBuilder Metrics => throw new NotImplementedException();

        // This one must be real as it is called during the execution process.
        IServiceProvider IHost.Services => new ServiceCollection()
                                                    .AddSingleton<IHostApplicationLifetime, HostApplicationLifetime>()
                                                    .BuildServiceProvider();

        IServiceCollection IHostApplicationBuilder.Services => throw new NotImplementedException();

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task WaitForShutdownAsync()
        {
            return this.stopped.Task;
        }

        public void Dispose()
        {
            this.IsDisposed = true;
        }

        public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
            where TContainerBuilder : notnull
        {
            throw new NotImplementedException();
        }
    }
}
