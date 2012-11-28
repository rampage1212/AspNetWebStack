﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web.Mvc.Filters;

namespace System.Web.Mvc.Async
{
    public class AsyncControllerActionInvoker : ControllerActionInvoker, IAsyncActionInvoker
    {
        private static readonly object _invokeActionTag = new object();
        private static readonly object _invokeActionMethodTag = new object();
        private static readonly object _invokeActionMethodWithFiltersTag = new object();

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Refactoring to reduce coupling not currently justified.")]
        public virtual IAsyncResult BeginInvokeAction(ControllerContext controllerContext, string actionName, AsyncCallback callback, object state)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (String.IsNullOrEmpty(actionName))
            {
                throw Error.ParameterCannotBeNullOrEmpty("actionName");
            }

            ControllerDescriptor controllerDescriptor = GetControllerDescriptor(controllerContext);
            ActionDescriptor actionDescriptor = FindAction(controllerContext, controllerDescriptor, actionName);
            if (actionDescriptor != null)
            {
                FilterInfo filterInfo = GetFilters(controllerContext, actionDescriptor);
                Action continuation = null;

                BeginInvokeDelegate beginDelegate = delegate(AsyncCallback asyncCallback, object asyncState)
                {
                    try
                    {
                        AuthenticationContext authenticationContext = InvokeAuthenticationFilters(controllerContext,
                            filterInfo.AuthenticationFilters, actionDescriptor);

                        if (authenticationContext.Result != null)
                        {
                            // An authentication filter signaled that we should short-circuit the request. Let all
                            // authentication filters contribute to an action result (to combine authentication
                            // challenges). Then, run this action result.
                            AuthenticationChallengeContext challengeContext =
                                InvokeAuthenticationFiltersChallenge(controllerContext,
                                filterInfo.AuthenticationFilters, actionDescriptor, authenticationContext.Result);
                            continuation = () => InvokeActionResult(controllerContext,
                                challengeContext.Result ?? authenticationContext.Result);
                        }
                        else
                        {
                            IPrincipal principal = authenticationContext.Principal;

                            if (principal != null)
                            {
                                Thread.CurrentPrincipal = principal;
                                HttpContext.Current.User = principal;
                            }

                            AuthorizationContext authorizationContext = InvokeAuthorizationFilters(controllerContext, filterInfo.AuthorizationFilters, actionDescriptor);
                            if (authorizationContext.Result != null)
                            {
                                // An authorization filter signaled that we should short-circuit the request. Let all
                                // authentication filters contribute to an action result (to combine authentication
                                // challenges). Then, run this action result.
                                AuthenticationChallengeContext challengeContext =
                                    InvokeAuthenticationFiltersChallenge(controllerContext,
                                    filterInfo.AuthenticationFilters, actionDescriptor, authorizationContext.Result);
                                continuation = () => InvokeActionResult(controllerContext, challengeContext.Result ??
                                    authorizationContext.Result);
                            }
                            else
                            {
                                if (controllerContext.Controller.ValidateRequest)
                                {
                                    ValidateRequest(controllerContext);
                                }

                                IDictionary<string, object> parameters = GetParameterValues(controllerContext, actionDescriptor);
                                IAsyncResult asyncResult = BeginInvokeActionMethodWithFilters(controllerContext, filterInfo.ActionFilters, actionDescriptor, parameters, asyncCallback, asyncState);
                                continuation = () =>
                                {
                                    ActionExecutedContext postActionContext = EndInvokeActionMethodWithFilters(asyncResult);
                                    // The action succeeded. Let all authentication filters contribute to an action
                                    // result (to combine authentication challenges; some authentication filters need
                                    // to do negotiation even on a successful result). Then, run this action result.
                                    AuthenticationChallengeContext challengeContext =
                                        InvokeAuthenticationFiltersChallenge(controllerContext,
                                        filterInfo.AuthenticationFilters, actionDescriptor,
                                        postActionContext.Result);
                                    InvokeActionResultWithFilters(controllerContext, filterInfo.ResultFilters,
                                        challengeContext.Result ?? postActionContext.Result);
                                };
                                return asyncResult;
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                        // the filters don't see this as an error.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // something blew up, so execute the exception filters
                        ExceptionContext exceptionContext = InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, ex);
                        if (!exceptionContext.ExceptionHandled)
                        {
                            throw;
                        }

                        continuation = () => InvokeActionResult(controllerContext, exceptionContext.Result);
                    }

                    return BeginInvokeAction_MakeSynchronousAsyncResult(asyncCallback, asyncState);
                };

                EndInvokeDelegate<bool> endDelegate = delegate(IAsyncResult asyncResult)
                {
                    try
                    {
                        continuation();
                    }
                    catch (ThreadAbortException)
                    {
                        // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                        // the filters don't see this as an error.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // something blew up, so execute the exception filters
                        ExceptionContext exceptionContext = InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, ex);
                        if (!exceptionContext.ExceptionHandled)
                        {
                            throw;
                        }
                        InvokeActionResult(controllerContext, exceptionContext.Result);
                    }

                    return true;
                };

                return AsyncResultWrapper.Begin(callback, state, beginDelegate, endDelegate, _invokeActionTag);
            }
            else
            {
                // Notify the controller that no action was found.
                return BeginInvokeAction_ActionNotFound(callback, state);
            }
        }

        private static IAsyncResult BeginInvokeAction_ActionNotFound(AsyncCallback callback, object state)
        {
            BeginInvokeDelegate beginDelegate = BeginInvokeAction_MakeSynchronousAsyncResult;

            EndInvokeDelegate<bool> endDelegate = delegate(IAsyncResult asyncResult)
            {
                return false;
            };

            return AsyncResultWrapper.Begin(callback, state, beginDelegate, endDelegate, _invokeActionTag);
        }

        private static IAsyncResult BeginInvokeAction_MakeSynchronousAsyncResult(AsyncCallback callback, object state)
        {
            SimpleAsyncResult asyncResult = new SimpleAsyncResult(state);
            asyncResult.MarkCompleted(true /* completedSynchronously */, callback);
            return asyncResult;
        }

        protected internal virtual IAsyncResult BeginInvokeActionMethod(ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
        {
            AsyncActionDescriptor asyncActionDescriptor = actionDescriptor as AsyncActionDescriptor;
            if (asyncActionDescriptor != null)
            {
                return BeginInvokeAsynchronousActionMethod(controllerContext, asyncActionDescriptor, parameters, callback, state);
            }
            else
            {
                return BeginInvokeSynchronousActionMethod(controllerContext, actionDescriptor, parameters, callback, state);
            }
        }

        protected internal virtual IAsyncResult BeginInvokeActionMethodWithFilters(ControllerContext controllerContext, IList<IActionFilter> filters, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
        {
            Func<ActionExecutedContext> endContinuation = null;

            BeginInvokeDelegate beginDelegate = delegate(AsyncCallback asyncCallback, object asyncState)
            {
                ActionExecutingContext preContext = new ActionExecutingContext(controllerContext, actionDescriptor, parameters);
                IAsyncResult innerAsyncResult = null;

                Func<Func<ActionExecutedContext>> beginContinuation = () =>
                {
                    innerAsyncResult = BeginInvokeActionMethod(controllerContext, actionDescriptor, parameters, asyncCallback, asyncState);
                    return () =>
                           new ActionExecutedContext(controllerContext, actionDescriptor, false /* canceled */, null /* exception */)
                           {
                               Result = EndInvokeActionMethod(innerAsyncResult)
                           };
                };

                // need to reverse the filter list because the continuations are built up backward
                Func<Func<ActionExecutedContext>> thunk = filters.Reverse().Aggregate(beginContinuation,
                                                                                      (next, filter) => () => InvokeActionMethodFilterAsynchronously(filter, preContext, next));
                endContinuation = thunk();

                if (innerAsyncResult != null)
                {
                    // we're just waiting for the inner result to complete
                    return innerAsyncResult;
                }
                else
                {
                    // something was short-circuited and the action was not called, so this was a synchronous operation
                    SimpleAsyncResult newAsyncResult = new SimpleAsyncResult(asyncState);
                    newAsyncResult.MarkCompleted(true /* completedSynchronously */, asyncCallback);
                    return newAsyncResult;
                }
            };

            EndInvokeDelegate<ActionExecutedContext> endDelegate = delegate(IAsyncResult asyncResult)
            {
                return endContinuation();
            };

            return AsyncResultWrapper.Begin(callback, state, beginDelegate, endDelegate, _invokeActionMethodWithFiltersTag);
        }

        private IAsyncResult BeginInvokeAsynchronousActionMethod(ControllerContext controllerContext, AsyncActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
        {
            BeginInvokeDelegate beginDelegate = delegate(AsyncCallback asyncCallback, object asyncState)
            {
                return actionDescriptor.BeginExecute(controllerContext, parameters, asyncCallback, asyncState);
            };

            EndInvokeDelegate<ActionResult> endDelegate = delegate(IAsyncResult asyncResult)
            {
                object returnValue = actionDescriptor.EndExecute(asyncResult);
                ActionResult result = CreateActionResult(controllerContext, actionDescriptor, returnValue);
                return result;
            };

            return AsyncResultWrapper.Begin(callback, state, beginDelegate, endDelegate, _invokeActionMethodTag);
        }

        private IAsyncResult BeginInvokeSynchronousActionMethod(ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
        {
            // Frequently called so ensure delegate remains static and arguments do not allocate
            EndInvokeDelegate<ActionInvocation, ActionResult> endInvokeFunc = (asyncResult, innerInvokeState) =>
                {
                    return innerInvokeState.InvokeSynchronousActionMethod();
                };
            ActionInvocation endInvokeState = new ActionInvocation(this, controllerContext, actionDescriptor, parameters);
            return AsyncResultWrapper.BeginSynchronous(callback, state, endInvokeFunc, endInvokeState, _invokeActionMethodTag);
        }

        public virtual bool EndInvokeAction(IAsyncResult asyncResult)
        {
            return AsyncResultWrapper.End<bool>(asyncResult, _invokeActionTag);
        }

        protected internal virtual ActionResult EndInvokeActionMethod(IAsyncResult asyncResult)
        {
            return AsyncResultWrapper.End<ActionResult>(asyncResult, _invokeActionMethodTag);
        }

        protected internal virtual ActionExecutedContext EndInvokeActionMethodWithFilters(IAsyncResult asyncResult)
        {
            return AsyncResultWrapper.End<ActionExecutedContext>(asyncResult, _invokeActionMethodWithFiltersTag);
        }

        protected override ControllerDescriptor GetControllerDescriptor(ControllerContext controllerContext)
        {
            Type controllerType = controllerContext.Controller.GetType();
            ControllerDescriptor controllerDescriptor = DescriptorCache.GetDescriptor(controllerType, () => new ReflectedAsyncControllerDescriptor(controllerType));
            return controllerDescriptor;
        }

        internal static Func<ActionExecutedContext> InvokeActionMethodFilterAsynchronously(IActionFilter filter, ActionExecutingContext preContext, Func<Func<ActionExecutedContext>> nextInChain)
        {
            filter.OnActionExecuting(preContext);
            if (preContext.Result != null)
            {
                ActionExecutedContext shortCircuitedPostContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, true /* canceled */, null /* exception */)
                {
                    Result = preContext.Result
                };
                return () => shortCircuitedPostContext;
            }

            // There is a nested try / catch block here that contains much the same logic as the outer block.
            // Since an exception can occur on either side of the asynchronous invocation, we need guards on
            // on both sides. In the code below, the second side is represented by the nested delegate. This
            // is really just a parallel of the synchronous ControllerActionInvoker.InvokeActionMethodFilter()
            // method.

            try
            {
                Func<ActionExecutedContext> continuation = nextInChain();

                // add our own continuation, then return the new function
                return () =>
                {
                    ActionExecutedContext postContext;
                    bool wasError = true;

                    try
                    {
                        postContext = continuation();
                        wasError = false;
                    }
                    catch (ThreadAbortException)
                    {
                        // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                        // the filters don't see this as an error.
                        postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, null /* exception */);
                        filter.OnActionExecuted(postContext);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, ex);
                        filter.OnActionExecuted(postContext);
                        if (!postContext.ExceptionHandled)
                        {
                            throw;
                        }
                    }
                    if (!wasError)
                    {
                        filter.OnActionExecuted(postContext);
                    }

                    return postContext;
                };
            }
            catch (ThreadAbortException)
            {
                // This type of exception occurs as a result of Response.Redirect(), but we special-case so that
                // the filters don't see this as an error.
                ActionExecutedContext postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, null /* exception */);
                filter.OnActionExecuted(postContext);
                throw;
            }
            catch (Exception ex)
            {
                ActionExecutedContext postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, ex);
                filter.OnActionExecuted(postContext);
                if (postContext.ExceptionHandled)
                {
                    return () => postContext;
                }
                else
                {
                    throw;
                }
            }
        }

        // Keep as value type to avoid per-call allocation
        private struct ActionInvocation
        {
            private readonly AsyncControllerActionInvoker _invoker;
            private readonly ControllerContext _controllerContext;
            private readonly ActionDescriptor _actionDescriptor;
            private readonly IDictionary<string, object> _parameters;

            internal ActionInvocation(AsyncControllerActionInvoker invoker, ControllerContext controllerContext, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
            {
                Contract.Assert(invoker != null);
                Contract.Assert(controllerContext != null);
                Contract.Assert(actionDescriptor != null);
                Contract.Assert(parameters != null);

                _invoker = invoker;
                _controllerContext = controllerContext;
                _actionDescriptor = actionDescriptor;
                _parameters = parameters;
            }

            internal ActionResult InvokeSynchronousActionMethod()
            {
                return _invoker.InvokeActionMethod(_controllerContext, _actionDescriptor, _parameters);
            }
        }
    }
}
