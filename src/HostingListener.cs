// <copyright file="HostingListener.cs" company="Create Your Software">
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
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Observes hosting diagnostic events emitted during application startup.
    /// </summary>
    /// <remarks>
    /// This listener is used by <see cref="HostApplicationFactory{TEntryPoint}"/>
    /// to intercept host construction, optionally configure the
    /// <see cref="IHostApplicationBuilder"/>, and capture the constructed
    /// <see cref="IHost"/> instance before the application enters its normal
    /// execution flow.
    ///
    /// <para>
    /// When the host is successfully constructed, the listener completes the
    /// provided <see cref="TaskCompletionSource{TResult}"/> with the constructed
    /// host and aborts the normal startup sequence of the application under test.
    /// Aborting the startup sequence allows <see cref="HostApplicationFactory{TEntryPoint}"/>
    /// to control host startup, shutdown, and inspection for integration testing.
    /// </para>
    /// </remarks>
    /// <param name="hostBuilder">
    /// An optional callback that is invoked when the host application builder is created.
    /// </param>
    /// <param name="hostCompletion">The completion source that receives the constructed <see cref="IHost"/> instance.
    /// </param>
    internal sealed class HostingListener(Action<IHostBuilder>? hostBuilder, TaskCompletionSource<IHost> hostCompletion)
                : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// The diagnostic listener name from which events are emitted.
        /// </summary>
        /// <remarks>
        /// This value is a copy of private const string
        /// HostBuildingDiagnosticListenerName = "Microsoft.Extensions.Hosting";
        /// in Microsoft.Extensions.Hosting because apparently they don't SHARE these
        /// constants even though consumers must know the value to subscribe to the
        /// events emitted by the hosting library.
        /// </remarks>
        public const string DIAGNOSTICS_LISTENER = "Microsoft.Extensions.Hosting";

        /// <summary>
        /// The diagnostic event raised when host construction begins.
        /// </summary>
        /// <remarks>
        /// This value is a copy of private const string HostBuildingEventName = "HostBuilding";
        /// in Microsoft.Extensions.Hosting because apparently they don't SHARE these
        /// constants even though consumers must know the value to subscribe to the
        /// events emitted by the hosting library.
        /// </remarks>
        public const string HOST_BUILDING = "HostBuilding";

        /// <summary>
        /// The diagnostic event raised after host construction completes.
        /// </summary>
        /// <remarks>
        /// This value is a copy of private const string HostBuiltEventName = "HostBuilt";
        /// in Microsoft.Extensions.Hosting because apparently they don't SHARE these
        /// constants even though consumers must know the value to subscribe to the
        /// events emitted by the hosting library.
        /// </remarks>
        public const string HOST_BUILT = "HostBuilt";

        private readonly Action<IHostBuilder>? hostBuilder = hostBuilder;

        private readonly TaskCompletionSource<IHost> hostCompletion = hostCompletion ?? throw new ArgumentNullException(nameof(hostCompletion));

        private IDisposable? hostingSubscription;

        /// <summary>
        /// Subscribes to hosting diagnostic events when the hosting diagnostic
        /// listener is observed.
        /// </summary>
        /// <param name="listener">
        /// The diagnostic listener being published.
        /// </param>
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == HostingListener.DIAGNOSTICS_LISTENER)
            {
                this.hostingSubscription =
                    listener.Subscribe(this);
            }
        }

        /// <summary>
        /// Processes hosting diagnostic events emitted during host construction.
        /// </summary>
        /// <param name="value">
        /// The diagnostic event and associated payload.
        /// </param>
        public void OnNext(KeyValuePair<string, object?> value)
        {
            switch (value.Key)
            {
                case HostingListener.HOST_BUILDING
                    when value.Value is IHostBuilder builder:
                    {
                        this.hostBuilder?.Invoke(builder);
                    }

                    break;

                case HostingListener.HOST_BUILT
                    when value.Value is IHost builtHost:
                    {
                        this.hostCompletion.TrySetResult(builtHost);
                    }

                    break;
            }
        }

        /// <summary>
        /// Ends observation of hosting diagnostic events.
        /// </summary>
        public void OnCompleted()
        {
            this.hostingSubscription?.Dispose();
        }

        /// <summary>
        /// Completes the host completion source with the observed exception.
        /// </summary>
        /// <param name="error">
        /// The exception reported by the diagnostic listener.
        /// </param>
        public void OnError(Exception error)
        {
            this.hostCompletion.TrySetException(error);
        }
    }
}
