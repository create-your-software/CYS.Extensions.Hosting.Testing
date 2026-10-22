// <copyright file="HostExtensionsTests.cs" company="Create Your Software">
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
    using CYS.SampleApp;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class HostExtensionsTests
    {
        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_When_StartAsync_Is_Skipped()
        {
            // arrange
            string[] args = [];
            var builder = Host.CreateApplicationBuilder(args);
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            builder.Services.AddHostedService<SampleProcessor>();

            string expectedMessage = "The host must be started before waiting for BackgroundService completion.";

            using (var host = builder.Build())
            {
                // Skip calling StartAsync().

                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.WaitUntilCompletionAsync(cancellationToken));

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }

        [TestMethod]
        public async Task Throws_InvalidOperationException_From_RunAsync_When_No_BackgroundService_Is_Configured()
        {
            // arrange
            string[] args = [];
            var builder = Host.CreateApplicationBuilder(args);
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            string expectedMessage = "The host contains no BackgroundService instances.";

            using (var host = builder.Build())
            {
                // act
                var result = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await host.WaitUntilCompletionAsync(cancellationToken));

                // assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedMessage, result.Message);
            }
        }
    }
}
