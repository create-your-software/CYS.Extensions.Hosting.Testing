// <copyright file="Program.cs" company="Create Your Software">
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

namespace CYS.SynchronousSampleApp
{
    using CYS.Extensions.Hosting.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;

    [ExcludeFromCodeCoverage]
    public class Program
    {
        protected Program()
        {
            // no op
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--target-invocation-exception")
            {
                throw new InvalidOperationException("target-invocation-exception");
            }

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService<SampleProcessor>();

            IHost? host = null;
            CancellationToken cancellationToken = default;

            try
            {
                if (args.Length > 0 && args[0] == "--host-not-built")
                {
                    throw new InvalidOperationException("host won't be built here.");
                }

                host = builder.Build();

                host.Start();

                host.WaitUntilCompletion(cancellationToken);

                host.Stop(cancellationToken);

                return 0;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                return -1;
            }
            finally
            {
                host?.Dispose();
            }
        }
    }
}
