﻿using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Common;

namespace System.Web.Http.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public abstract class ExceptionFilterAttribute : FilterAttribute, IExceptionFilter
    {
        public virtual void OnException(HttpActionExecutedContext actionExecutedContext)
        {
        }

        Task IExceptionFilter.ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext == null)
            {
                throw Error.ArgumentNull("actionExecutedContext");
            }

            OnException(actionExecutedContext);
            return TaskHelpers.Completed();
        }
    }
}
