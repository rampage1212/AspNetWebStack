﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Properties;

namespace System.Web.Http.ExceptionHandling
{
    /// <summary>Represents an unhandled exception logger.</summary>
    public abstract class ExceptionLogger : IExceptionLogger
    {
        internal const string LoggedByKey = "MS_LoggedBy";

        /// <inheritdoc />
        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.ExceptionContext == null)
            {
                throw new ArgumentException(Error.Format(SRResources.TypePropertyMustNotBeNull,
                    typeof(ExceptionLoggerContext).Name, "ExceptionContext"), "context");
            }

            if (context.ExceptionContext.Exception == null)
            {
                throw new ArgumentException(Error.Format(SRResources.TypePropertyMustNotBeNull,
                    typeof(ExceptionContext).Name, "Exception"), "context");
            }

            if (!ShouldLog(context))
            {
                return Task.FromResult<object>(null);
            }

            return LogAsyncCore(context, cancellationToken);
        }

        /// <summary>When overridden in a derived class, logs the exception asynchronously.</summary>
        /// <param name="context">The exception logger context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task LogAsyncCore(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            LogCore(context);
            return Task.FromResult<object>(null);
        }

        /// <summary>When overridden in a derived class, logs the exception synchronously.</summary>
        /// <param name="context">The exception logger context.</param>
        public virtual void LogCore(ExceptionLoggerContext context)
        {
        }

        /// <summary>Determines whether the exception should be logged.</summary>
        /// <param name="context">The exception logger context.</param>
        /// <returns>
        /// <see langword="true"/> if the exception should be logged; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The default decision is only to log an exception instance the first time it is seen by this logger.
        /// </remarks>
        public virtual bool ShouldLog(ExceptionLoggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ExceptionContext exceptionContext = context.ExceptionContext;

            if (exceptionContext == null)
            {
                throw new ArgumentException(Error.Format(SRResources.TypePropertyMustNotBeNull,
                    typeof(ExceptionLoggerContext).Name, "ExceptionContext"), "context");
            }

            Exception exception = exceptionContext.Exception;

            if (exception == null)
            {
                throw new ArgumentException(Error.Format(SRResources.TypePropertyMustNotBeNull,
                    typeof(ExceptionContext).Name, "Exception"), "context");
            }

            IDictionary data = exception.Data;

            if (data == null || data.IsReadOnly)
            {
                // If the exception doesn't have a mutable Data collection, we can't prevent duplicate logging. In this
                // case, just log every time.
                return true;
            }

            ICollection<object> loggedBy;

            if (data.Contains(LoggedByKey))
            {
                object untypedLoggedBy = data[LoggedByKey];

                loggedBy = untypedLoggedBy as ICollection<object>;

                if (loggedBy == null)
                {
                    // If exception.Data["MS_LoggedBy"] exists but is not of the right type, we can't prevent duplicate
                    // logging. In this case, just log every time.
                    return true;
                }

                if (loggedBy.Contains(this))
                {
                    // If this logger has already logged this exception, don't log again.
                    return false;
                }
            }
            else
            {
                loggedBy = new List<object>();
                data.Add(LoggedByKey, loggedBy);
            }

            // Either loggedBy did not exist before (we just added it) or it already existed of the right type and did
            // not already contain this logger. Log now, but mark not to log this exception again for this logger.
            Contract.Assert(loggedBy != null);
            loggedBy.Add(this);
            return true;
        }
    }
}
