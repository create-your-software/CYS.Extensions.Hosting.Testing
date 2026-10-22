// <copyright file="HostApplicationLifetime.cs" company="Create Your Software">
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
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    [ExcludeFromCodeCoverage]
    internal sealed class HostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource started = new();
        private readonly CancellationTokenSource stopping = new();
        private readonly CancellationTokenSource stopped = new();

        public CancellationToken ApplicationStarted =>
            this.started.Token;

        public CancellationToken ApplicationStopping =>
            this.stopping.Token;

        public CancellationToken ApplicationStopped =>
            this.stopped.Token;

        public void NotifyStarted()
        {
            this.started.Cancel();
        }

        public void StopApplication()
        {
            this.stopping.Cancel();
        }

        public void NotifyStopped()
        {
            this.stopped.Cancel();
        }

        public void Dispose()
        {
            this.started.Dispose();
            this.stopping.Dispose();
            this.stopped.Dispose();
        }
    }
}
