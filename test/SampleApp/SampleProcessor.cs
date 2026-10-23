// <copyright file="SampleProcessor.cs" company="Create Your Software">
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

namespace CYS.SampleApp
{
    using Microsoft.Extensions.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    public class SampleProcessor() : BackgroundService
    {
        public virtual int ReturnValue => 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Trying to discover a race condition in .NET 9.0.
            await Task.Yield();
        }
    }
}
