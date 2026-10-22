// <copyright file="HostApplicationFactoryTests.cs" company="Create Your Software">
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

namespace CYS.Extensions.Hosting.Testing.Tests
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using CYS.Extensions.Hosting.Testing;
    using CYS.SampleApp;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    [TestClass]
    public sealed class HostApplicationFactoryTests
    {
#pragma warning disable S2699 // Assert at least one test.
#pragma warning disable S6966 // Calling Dispose first.
        [TestMethod]
        public void Returns_No_Errors_From_Dispose_When_Called_More_Than_Once()
        {
            // arrange
            string[] args = [];

            var host = new HostApplicationFactory<SampleApp.Program>(args);
            host.Dispose();
            host.Dispose();
        }
#pragma warning restore S6966

        [TestMethod]
        public async Task Returns_No_Errors_From_DisposeAsync_When_Called_More_Than_Once()
        {
            // arrange
            string[] args = [];

            var host = new HostApplicationFactory<SampleApp.Program>(args);
            await host.DisposeAsync();
            await host.DisposeAsync();
        }
#pragma warning restore S2699

        [TestMethod]
        public async Task Returns_No_Errors_From_DisposeAsync_When_A_Premature_Disposal_From_HostApplicationFactory_Happens_Before_EntryPoint_Completion()
        {
            // arrange
            string[] args = [];
            int expectedResult = 0;

            var host = new HostApplicationFactory<PrematureDisposalApp.Program>(args);

            Task<int?> runTask = host.RunAsync();

            // act
            await host.DisposeAsync();

            int? result = await runTask;

            // assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task Returns_Zero_From_RunAsync_When_A_SampleProcessor_Runs_To_Successful_Completion()
        {
            // arrange
            string[] args = [];
            int expectedResult = 0;

            using (var host = new HostApplicationFactory<SampleApp.Program>(args))
            {
                // act
                var result = await host.RunAsync();

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedResult, result.Value);
            }
        }

        [TestMethod]
        public async Task Returns_Seven_From_RunAsync_When_Using_Autofac()
        {
            // arrange
            string[] args = [];
            int expectedResult = 7;

            using var host =
                new HostApplicationFactory<SampleApp.Program>(args)
                    .WithHostBuilder(
                        builder =>
                        {
                            // Prevent AutofacServiceProviderFactory from importing
                            // the original SampleProcessor registration.
                            builder.Services.RemoveAll<IHostedService>();

                            builder.ConfigureContainer(
                                new AutofacServiceProviderFactory(),
                                containerBuilder =>
                                {
                                    containerBuilder
                                        .RegisterType<SampleOverrideProcessor>()
                                        .As<IHostedService>()
                                        .SingleInstance();
                                });
                        });

            // act
            var result = await host.RunAsync();

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result.Value);
        }

        [TestMethod]
        public async Task Returns_Zero_From_RunAsync_When_A_Asynchronous_Second_SampleProcessor_Runs_To_Successful_Completion()
        {
            // arrange
            string[] args = [];
            int expectedResult = 0;

            using (var host = new HostApplicationFactory<SecondSampleApp.Program>(args))
            {
                // act
                var result = await host.RunAsync();

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedResult, result.Value);
            }
        }

        [TestMethod]
        public async Task Returns_Seven_From_RunAsync_When_An_Overridden_SampleProcessor()
        {
            // arrange
            string[] args = [];
            int expectedResult = 7;

            using (var host = new HostApplicationFactory<SampleApp.Program>(args)
                                        .WithHostBuilder((builder) =>
                                            {
                                                builder.Services.RemoveAll<IHostedService>();
                                                builder.Services.AddHostedService<SampleOverrideProcessor>();
                                            }))
            {
                // act
                var result = await host.RunAsync();

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedResult, result.Value);
            }
        }

        [TestMethod]
        public async Task Returns_Zero_From_RunAsync_When_A_SynchronousSampleProcessor_Runs_To_Successful_Completion()
        {
            // arrange
            string[] args = [];
            int expectedResult = 0;

            using (var host = new HostApplicationFactory<SynchronousSampleApp.Program>(args))
            {
                // act
                var result = await host.RunAsync();

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedResult, result.Value);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_Program_Does_Not_Have_EntryPoint()
        {
            // arrange
            string[] args = ["--host-not-built"];
            string expectedMessage = "Assembly 'SampleLibrary, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null' does not have an entry point.";

            using (var host = new HostApplicationFactory<SampleLibrary.Program>(args))
            {
                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.RunAsync());

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_When_Inner_Host_Is_Not_Built()
        {
            // arrange
            string[] args = ["--host-not-built"];
            string expectedMessage = "The entry point for the application under test completed without building an IHost.";

            using (var host = new HostApplicationFactory<SampleApp.Program>(args))
            {
                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.RunAsync());

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_To_Create_TargetInvocationException()
        {
            // arrange
            string[] args = ["--target-invocation-exception"];
            string expectedMessage = "target-invocation-exception";

            using (var host = new HostApplicationFactory<SynchronousSampleApp.Program>(args))
            {
                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.RunAsync());

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_When_RunAsync_Is_Called_A_Second_Time()
        {
            // arrange
            string[] args = [];
            string expectedMessage = "The application under test is already running or has completed.";

            using (var host = new HostApplicationFactory<SampleApp.Program>(args))
            {
                _ = await host.RunAsync();

                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.RunAsync());

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_WithHostBuilder_When_WithHostBuilder_Is_Called_Twice()
        {
            // arrange
            string[] args = [];
            var expectedMessage = "WithHostBuilder can only be called once per instance.";

            var host = new HostApplicationFactory<SampleApp.Program>(args)
                                        .WithHostBuilder((builder) =>
                                        {
                                            builder.Services.RemoveAll<IHostedService>();
                                            builder.Services.AddHostedService<SampleOverrideProcessor>();
                                        });

            // act
            var result = Assert.ThrowsExactly<InvalidOperationException>(() => host.WithHostBuilder((builder) => { }));

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedMessage, result.Message);
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_WithHostBuilder_When_RunAsync_Is_Called_A_Second_Time()
        {
            // arrange
            string[] args = [];
            var expectedMessage = "The factory cannot be configured after RunAsync() has been called.";

            using (var host = new HostApplicationFactory<SampleApp.Program>(args))
            {
                _ = await host.RunAsync();

                // act
                var result = Assert.ThrowsExactly<InvalidOperationException>(() => host.WithHostBuilder((builder) => { }));

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }
    }
}