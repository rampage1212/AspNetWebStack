﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Web.Http.Controllers;

namespace System.Web.Http.ExceptionHandling
{
    /// <summary>Creates exception services to call logging and handling from catch blocks.</summary>
    public static class ExceptionServices
    {
        /// <summary>Gets an exception logger that calls all registered logger services.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>A composite logger.</returns>
        public static IExceptionLogger GetLogger(HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            ServicesContainer services = configuration.Services;
            Contract.Assert(services != null);
            return GetLogger(services);
        }

        internal static IExceptionLogger GetLogger(ServicesContainer services)
        {
            Contract.Assert(services != null);

            return services.ExceptionServicesLogger.Value;
        }

        internal static IExceptionLogger CreateLogger(ServicesContainer services)
        {
            Contract.Assert(services != null);

            IEnumerable<IExceptionLogger> loggers = services.GetExceptionLoggers();
            Contract.Assert(loggers != null);
            return new CompositeExceptionLogger(loggers);
        }

        /// <summary>
        /// Gets an exception handler that calls the registered handler service, if any, and ensures exceptions do not
        /// accidentally propagate to the host.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// An exception handler that calls any registered handler and ensures exceptions do not accidentally propagate
        /// to the host.
        /// </returns>
        public static IExceptionHandler GetHandler(HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            ServicesContainer services = configuration.Services;
            Contract.Assert(services != null);
            return GetHandler(services);
        }

        internal static IExceptionHandler GetHandler(ServicesContainer services)
        {
            Contract.Assert(services != null);

            return services.ExceptionServicesHandler.Value;
        }

        internal static IExceptionHandler CreateHandler(ServicesContainer services)
        {
            Contract.Assert(services != null);

            IExceptionHandler innerHandler = services.GetExceptionHandler() ?? new EmptyExceptionHandler();
            return new LastChanceExceptionHandler(innerHandler);
        }
    }
}
