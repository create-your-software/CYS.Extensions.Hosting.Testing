// <copyright file="HostApplicationFactory{TEntryPoint}.cs" company="Create Your Software">
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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an in-process factory for invoking and testing a host-based application
    /// through its entry point.
    /// </summary>
    /// <typeparam name="TEntryPoint">
    /// A type contained in the entry-point assembly of the application under test.
    /// Typically, the application's Program type is used.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Unlike testing approaches that bypass the application entry point, this factory invokes the
    /// entry point directly so that its complete implementation participates in integration testing
    /// and can be measured by code coverage tooling.
    /// </para>
    /// <para>
    /// The factory captures and exposes the <see cref="IHost"/> built by the application under test,
    /// including any test-specific configuration supplied through <see cref="WithHostBuilder(Action{IHostApplicationBuilder})"/>.
    /// Disposing the factory stops and disposes the captured host.
    /// </para>
    /// <para>
    /// Each factory instance supports one application execution.
    /// Concurrent calls involving <see cref="RunAsync(CancellationToken)"/>, <see cref="Dispose"/>, or
    /// <see cref="DisposeAsync"/> are not supported.
    /// </para>
    /// </remarks>
    public sealed class HostApplicationFactory<TEntryPoint> : IAsyncDisposable, IDisposable
        where TEntryPoint : class
    {
        /// <summary>
        /// Represents the command-line arguments passed to the application entry point.
        /// </summary>
        private readonly string[] arguments;

        /// <summary>
        /// Synchronizes host configuration and the transition into application
        /// execution.
        /// </summary>
#if NET9_0_OR_GREATER
        private readonly System.Threading.Lock syncRoot = new();
#else
        private readonly object syncRoot = new();
#endif

        /// <summary>
        /// Represents the optional action used to configure the <see cref="IHostBuilder"/>
        /// before the application host is built.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Diagnostics Listener requires an <see cref="IHostBuilder"/>.
        /// </para>
        /// <para>
        /// The caller's interface is going to use <see cref="IHostApplicationBuilder"/> to preserve the modern API.
        /// </para>
        /// </remarks>
        private Action<IHostBuilder>? hostBuilder;

        /// <summary>
        /// Indicates whether this factory has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Indicates whether <see cref="RunAsync(CancellationToken)"/> has been called.
        /// </summary>
        private bool runAsyncCalled;

        // This class mirrors the WebApplicationFactory{TEntryPoint} implementation;
        // its constructor documentation follows the same approach.
#pragma warning disable SA1642 // Matching constructor documentation from WebApplicationFactory{TEntryPoint}.
        /// <summary>
        /// <para>
        /// Creates an instance of <see cref="HostApplicationFactory{TEntryPoint}"/> that invokes the
        /// application entry point in the assembly containing <typeparamref name="TEntryPoint"/> and
        /// captures the resulting <see cref="IHost"/>.
        /// </para>
        /// <para>
        /// Invoking the actual entry point allows the complete entry-point implementation to participate
        /// in integration testing and to be measured by code coverage tooling.
        /// tooling.
        /// </para>
        /// <para>
        /// The application assemblies are loaded from the dependency context of the assembly containing
        /// <typeparamref name="TEntryPoint"/>.
        /// </para>
        /// </summary>
        /// <param name="arguments">The command-line arguments passed to the application entry point.</param>
        public HostApplicationFactory(params string[] arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);

            // Prevents later mutations by the caller.
            this.arguments = [.. arguments];
        }
#pragma warning restore SA1642

        /// <summary>
        /// Gets the <see cref="IHost"/> built by the application under test,
        /// including any test-specific configuration supplied through
        /// <see cref="WithHostBuilder(Action{IHostApplicationBuilder})"/>.
        /// </summary>
        /// <remarks>
        /// The value is <see langword="null"/> until the application has built its <see cref="IHost"/>.
        /// Disposing this factory stops and disposes the captured <see cref="IHost"/>.
        /// </remarks>
        public IHost? Host { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            try
            {
                if (this.Host is null)
                {
                    return;
                }

                // HostingListener captures the framework host from the
                // Microsoft.Extensions.Hosting "HostBuilt" diagnostic event.
                // Because HostApplicationFactory cannot substitute a different
                // IHost instance, the captured host is always the framework
                // implementation, which implements IAsyncDisposable.
                await (this.Host as IAsyncDisposable)!
                    .DisposeAsync()
                    .ConfigureAwait(false);
            }
            finally
            {
                this.disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Configures the <see cref="IHostApplicationBuilder"/> with test-specific
        /// services and configuration before the application host is built.
        /// </summary>
        /// <param name="configuration">The action used to configure the <see cref="IHostApplicationBuilder"/>.</param>
        /// <returns>
        /// The current <see cref="HostApplicationFactory{TEntryPoint}"/> instance with the supplied configuration.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this factory has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this method has already been called or <see cref="RunAsync(CancellationToken)"/> has been called.
        /// </exception>
        public HostApplicationFactory<TEntryPoint> WithHostBuilder(Action<IHostApplicationBuilder> configuration)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            ArgumentNullException.ThrowIfNull(configuration);

            lock (this.syncRoot)
            {
                if (this.hostBuilder is not null)
                {
                    throw new InvalidOperationException(
                        "WithHostBuilder can only be called once per instance.");
                }

                if (this.runAsyncCalled)
                {
                    throw new InvalidOperationException(
                        "The factory cannot be configured after RunAsync() has been called.");
                }

                this.hostBuilder =
                    hostBuilder =>
                    {
                        hostBuilder.ConfigureServices(
                            (context, services) =>
                            {
                                var facade =
                                    new HostApplicationBuilderFacade(
                                        hostBuilder,
                                        context,
                                        services);

                                configuration(facade);
                            });
                    };
            }

            return this;
        }

        /// <summary>
        /// Invokes the entry point and runs the host-based application under test to
        /// completion.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token that cancels host startup or waiting for the application entry point to complete.
        /// </param>
        /// <returns>
        /// A task whose result is <see langword="null"/> when the application entry point returns
        /// <see langword="void"/> or <see cref="Task"/>; otherwise, the result is the exit code
        /// returned by an <see langword="int"/> or <see cref="Task{Int32}"/> entry point.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when this factory has been disposed.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="RunAsync(CancellationToken)"/> has already been called on this factory.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="cancellationToken"/> is canceled during host startup
        /// or while waiting for the application entry point to complete.
        /// </exception>
        public async Task<int?> RunAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            lock (this.syncRoot)
            {
                if (this.runAsyncCalled)
                {
                    throw new InvalidOperationException("The application under test is already running or has completed.");
                }

                this.runAsyncCalled = true;
            }

            var systemUnderTest = await this.CreateHostAsync().ConfigureAwait(false);

            this.Host = systemUnderTest.Host;

            var result = await systemUnderTest.EntryPointTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }

        [ExcludeFromCodeCoverage]
        private static void ThrowOnUnsignedIntegerReturnValue(object? result)
        {
            if (result is uint)
            {
                throw new NotSupportedException("While the ECMA Specification permits an unsigned integer return value from an entry point method, this library does not support it.");
            }
        }

        private async Task<(IHost Host, Task<int?> EntryPointTask)> CreateHostAsync()
        {
            var assembly = typeof(TEntryPoint).Assembly;

            var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException($"Assembly '{assembly.FullName}' does not have an entry point.");

            var hostCompletion =
                new TaskCompletionSource<IHost>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            var listener =
                new HostingListener(
                    this.hostBuilder,
                    hostCompletion);

            using var subscription =
                DiagnosticListener.AllListeners.Subscribe(listener);

            var resultEntryPointTask =
                Task.Run(
                    () => this.InvokeEntryPointAsync(
                        entryPoint,
                        hostCompletion),
                    CancellationToken.None);

            var resultHost =
                await hostCompletion.Task.ConfigureAwait(false);

            return (resultHost, resultEntryPointTask);
        }

        private async Task<int?> InvokeEntryPointAsync(MethodInfo entryPoint, TaskCompletionSource<IHost> hostCompletion)
        {
            try
            {
                var parameters = entryPoint.GetParameters();

                // The outer collection expression passes this.arguments as the entry point's
                // single parameter rather than as the argument list supplied to Invoke().
                //
                // According to the ECMA CLI Specification, an entry point method must have
                // either no parameters or a single string[] parameter containing the command
                // line arguments. Therefore, the only valid invocation forms are:
                //
                //     static int Main()
                //     static int Main(string[] args)
                //
                // (with C# async entry points lowered to a CLR-compatible entry point).
#pragma warning disable CS8509
                object? result = parameters.Length switch
                {
                    0 => entryPoint.Invoke(null, null),
                    1 when parameters[0].ParameterType == typeof(string[]) =>
                                entryPoint.Invoke(null, [this.arguments]),
                };
#pragma warning restore CS8509

                HostApplicationFactory<TEntryPoint>.ThrowOnUnsignedIntegerReturnValue(result);

                int? exitCode = (int?)result;

                if (!hostCompletion.Task.IsCompleted)
                {
                    hostCompletion.TrySetException(new InvalidOperationException("The entry point for the application under test completed without building an IHost."));
                }

                return exitCode;
            }
            catch (Exception exception)
            {
                // Exceptions thrown by a normally compiled entry point are wrapped in a
                // TargetInvocationException. A separate general exception handler would require
                // an IL- or reflection-level failure to cover, so both exception forms are
                // normalized and handled here.
                var actualException = this.PrepareException(exception);

                hostCompletion.TrySetException(actualException);

                // Rethrow the underlying exception when reflection wrapped it; otherwise,
                // rethrow the original exception. ExceptionDispatchInfo preserves the
                // original stack trace.
                ExceptionDispatchInfo.Capture(actualException).Throw();

                // This line is required for compiler control-flow analysis
                // but can never execute and therefore cannot be covered.
                throw;
            }
        }

        // Simplify the code coverage by moving this into a separate method.
        [ExcludeFromCodeCoverage]
        private Exception PrepareException(Exception exception)
        {
            Exception result =
                exception is TargetInvocationException { InnerException: not null }
                ? exception.InnerException
                : exception;

            return result;
        }
    }
}
