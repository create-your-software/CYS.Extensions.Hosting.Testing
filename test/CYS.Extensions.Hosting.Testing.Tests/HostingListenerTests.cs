// <copyright file="HostingListenerTests.cs" company="Create Your Software">
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
    using Microsoft.Extensions.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class HostingListenerTests
    {
#pragma warning disable S2699 // Test success is no exception thrown; no assert needed.
        [TestMethod]
        public void Returns_Successfully_From_OnCompleted()
        {
            // arrange
            var hostBuilder = Mock.Of<Action<IHostBuilder>>(MockBehavior.Strict);
            var hostCompletion = new TaskCompletionSource<IHost>();

            var sut = new HostingListener(hostBuilder, hostCompletion);

            // act
            sut.OnCompleted();

            // assert
            // No exception thrown means test was successful.
        }
#pragma warning restore S2699

        [TestMethod]
        public void Returns_Successfully_From_OnError()
        {
            // arrange
            var hostBuilder = Mock.Of<Action<IHostBuilder>>(MockBehavior.Strict);
            var hostCompletion = new TaskCompletionSource<IHost>();
            var exception = new MissingFieldException();

            var sut = new HostingListener(hostBuilder, hostCompletion);

            // act
            sut.OnError(exception);

            // assert
            Assert.AreEqual(exception, hostCompletion.Task.Exception?.InnerException);
        }

        [TestMethod]
        public void Throws_ArgumentNulLException_From_Constructor_When_HostCompletion_Parameter_Is_Null()
        {
            // arrange
            Action<IHostBuilder>? hostBuilder = null;
            TaskCompletionSource<IHost>? hostCompletion = null;

            var expectedParamName = nameof(hostCompletion);

            // act
            var result = Assert.ThrowsExactly<ArgumentNullException>(() => new HostingListener(hostBuilder, hostCompletion!));

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedParamName, result.ParamName);
        }
    }
}
