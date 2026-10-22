// <copyright file="HostExtensions.cs" company="Create Your Software">
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
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides synchronous convenience methods for starting, monitoring,
    /// and stopping an <see cref="IHost"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods bridge asynchronous host lifecycle operations into synchronous
    /// calls by blocking the calling thread until the underlying operation completes.
    /// </para>
    /// <para>
    /// The caller is responsible for selecting execution tasks that can complete
    /// independently. Do not pass a task to <c>WaitUntilCompletion</c> when that task
    /// requires the host to stop or its stopping token to be cancelled before it
    /// can complete.
    /// </para>
    /// <para>While Microsoft provided a synchronous-to-asynchronous mapping for <see cref="IHost.StartAsync(CancellationToken)"/>,
    /// they inexplicably did not create a wrapper method to capture the waiting state or handle the Stop() method.</para>
    /// </remarks>
    public static class HostExtensions
    {
        /// <summary>
        /// Stops the host and blocks until shutdown completes.
        /// </summary>
        /// <param name="host">The host to stop synchronously.</param>
        /// <param name="cancellationToken">A cancellation token passed to <see cref="IHost.StopAsync(CancellationToken)"/>.</param>
        public static void Stop(this IHost host, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(host);

            host.StopAsync(cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Blocks until all specified execution tasks complete.
        /// </summary>
        /// <remarks>
        /// Do not pass an execution task that requires host shutdown to complete.
        /// The caller controls which hosted-service tasks are awaited.
        /// </remarks>
        /// <param name="host">The host from which the tasks originated.</param>
        /// <param name="cancellationToken">A cancellation token passed to <see cref="Task.WaitAsync(CancellationToken)"/>.</param>
        public static void WaitUntilCompletion(this IHost host, CancellationToken cancellationToken = default)
        {
            HostExtensions.WaitUntilCompletionAsync(host, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Blocks until all specified execution tasks complete.
        /// </summary>
        /// <remarks>
        /// Do not pass an execution task that requires host shutdown to complete.
        /// The caller controls which hosted-service tasks are awaited.
        /// </remarks>
        /// <param name="host">The host from which the tasks originated.</param>
        /// <param name="cancellationToken">A cancellation token passed to <see cref="Task.WaitAsync(CancellationToken)"/>.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public static async Task WaitUntilCompletionAsync(this IHost host, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(host);

            var processors = host.Services
                                    .GetServices<IHostedService>()
                                    .OfType<BackgroundService>()
                                    .ToList();

            if (processors.Count == 0)
            {
                throw new InvalidOperationException("The host contains no BackgroundService instances.");
            }

            if (processors.Any(x => x.ExecuteTask is null))
            {
                throw new InvalidOperationException("The host must be started before waiting for BackgroundService completion.");
            }

            var tasks = new List<Task>();

            foreach (var processor in processors)
            {
                tasks.Add(processor.ExecuteTask!);
            }

            await Task.WhenAll(tasks)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
        }
    }
}
