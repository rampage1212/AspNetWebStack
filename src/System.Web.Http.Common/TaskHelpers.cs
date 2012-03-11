﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Helpers for safely using Task libraries. 
    /// </summary>
    internal static class TaskHelpers
    {
        private static Task _defaultCompleted = FromResult<AsyncVoid>(default(AsyncVoid));

        /// <summary>
        /// Returns a canceled Task. The task is completed, IsCanceled = True, IsFaulted = False.
        /// </summary>
        internal static Task Canceled()
        {
            return CancelCache<AsyncVoid>.Canceled;
        }

        /// <summary>
        /// Returns a canceled Task of the given type. The task is completed, IsCanceled = True, IsFaulted = False.
        /// </summary>
        internal static Task<TResult> Canceled<TResult>()
        {
            return CancelCache<TResult>.Canceled;
        }

        /// <summary>
        /// Returns a completed task that has no result. 
        /// </summary>        
        internal static Task Completed()
        {
            return _defaultCompleted;
        }

        /// <summary>
        /// Returns an error task. The task is Completed, IsCanceled = False, IsFaulted = True
        /// </summary>
        internal static Task FromError(Exception exception)
        {
            return FromError<AsyncVoid>(exception);
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        internal static Task<TResult> FromError<TResult>(Exception exception)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True
        /// </summary>
        internal static Task FromErrors(IEnumerable<Exception> exceptions)
        {
            return FromErrors<AsyncVoid>(exceptions);
        }

        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True
        /// </summary>
        internal static Task<TResult> FromErrors<TResult>(IEnumerable<Exception> exceptions)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exceptions);
            return tcs.Task;
        }

        /// <summary>
        /// Returns a successful completed task with the given result.  
        /// </summary>        
        internal static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        /// <summary>
        /// Return a task that runs all the tasks inside the iterator sequentially. It stops as soon
        /// as one of the tasks fails or cancels, or after all the tasks have run succesfully.
        /// </summary>
        /// <param name="asyncIterator">collection of tasks to wait on</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>a task that signals completed when all the incoming tasks are finished.</returns>
        internal static Task Iterate(IEnumerable<Task> asyncIterator, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Assert(asyncIterator != null);

            return IterateEngine.Run(asyncIterator, cancellationToken);
        }

        /// <summary>
        /// Return a task that runs all the tasks inside the iterator sequentially and collects the results.
        /// It stops as soon as one of the tasks fails or cancels, or after all the tasks have run succesfully.
        /// </summary>
        /// <param name="asyncIterator">collection of tasks to wait on</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>A task that, upon successful completion, returns the list of results.</returns>
        internal static Task<IEnumerable<TResult>> Iterate<TResult>(IEnumerable<Task<TResult>> asyncIterator, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Assert(asyncIterator != null);

            return IterateEngine<TResult>.Run(asyncIterator, cancellationToken);
        }

        /// <summary>
        /// Replacement for Task.Factory.StartNew when the code can run synchronously. 
        /// We run the code immediately and avoid the thread switch. 
        /// This is used to help synchronous code implement task interfaces.
        /// </summary>
        /// <param name="action">action to run synchronouslyt</param>
        /// <param name="token">cancellation token. This is only checked before we run the task, and if cancelled, we immediately return a cancelled task.</param>
        /// <returns>a task who result is the result from Func()</returns>
        /// <remarks>
        /// Avoid calling Task.Factory.StartNew.         
        /// This avoids gotchas with StartNew:
        /// - ensures cancellation token is checked (StartNew doesn't check cancellation tokens).
        /// - Keeps on the same thread. 
        /// - Avoids switching synchronization contexts.
        /// Also take in a lambda so that we can wrap in a try catch and honor task failure semantics.        
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The caught exception type is reflected into a faulted task.")]
        public static Task RunSynchronously(Action action, CancellationToken token = default(CancellationToken))
        {
            if (token.IsCancellationRequested)
            {
                return Canceled();
            }

            try
            {
                action();
                return Completed();
            }
            catch (Exception e)
            {
                return FromError(e);
            }
        }

        /// <summary>
        /// Replacement for Task.Factory.StartNew when the code can run synchronously. 
        /// We run the code immediately and avoid the thread switch. 
        /// This is used to help synchronous code implement task interfaces.
        /// </summary>
        /// <typeparam name="TResult">type of result that task will return.</typeparam>
        /// <param name="func">function to run synchronously and produce result</param>
        /// <param name="cancellationToken">cancellation token. This is only checked before we run the task, and if cancelled, we immediately return a cancelled task.</param>
        /// <returns>a task who result is the result from Func()</returns>
        /// <remarks>
        /// Avoid calling Task.Factory.StartNew.         
        /// This avoids gotchas with StartNew:
        /// - ensures cancellation token is checked (StartNew doesn't check cancellation tokens).
        /// - Keeps on the same thread. 
        /// - Avoids switching synchronization contexts.
        /// Also take in a lambda so that we can wrap in a try catch and honor task failure semantics.        
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The caught exception type is reflected into a faulted task.")]
        internal static Task<TResult> RunSynchronously<TResult>(Func<TResult> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Canceled<TResult>();
            }

            try
            {
                return FromResult(func());
            }
            catch (Exception e)
            {
                return FromError<TResult>(e);
            }
        }

        /// <summary>
        /// Overload of RunSynchronously that avoids a call to Unwrap(). 
        /// This overload is useful when func() starts doing some synchronous work and then hits IO and 
        /// needs to create a task to finish the work. 
        /// </summary>
        /// <typeparam name="TResult">type of result that Task will return</typeparam>
        /// <param name="func">function that returns a task</param>
        /// <param name="cancellationToken">cancellation token. This is only checked before we run the task, and if cancelled, we immediately return a cancelled task.</param>
        /// <returns>a task, created by running func().</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The caught exception type is reflected into a faulted task.")]
        internal static Task<TResult> RunSynchronously<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Canceled<TResult>();
            }

            try
            {
                return func();
            }
            catch (Exception e)
            {
                return FromError<TResult>(e);
            }
        }

        /// <summary>
        /// Update the completion source if the task failed (cancelled or faulted). No change to completion source if the task succeeded. 
        /// </summary>
        /// <typeparam name="TResult">result type of completion source</typeparam>
        /// <param name="tcs">completion source to update</param>
        /// <param name="source">task to update from.</param>
        /// <returns>true on success</returns>
        internal static bool SetIfTaskFailed<TResult>(this TaskCompletionSource<TResult> tcs, Task source)
        {
            switch (source.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    return tcs.TrySetFromTask(source);
            }

            return false;
        }

        /// <summary>
        /// Set a completion source from the given Task.
        /// </summary>
        /// <typeparam name="TResult">result type for completion source.</typeparam>
        /// <param name="tcs">completion source to set</param>
        /// <param name="source">Task to get values from.</param>
        /// <returns>true if this successfully sets the completion source.</returns>
        [SuppressMessage("Microsoft.WebAPI", "CR4001:DoNotCallProblematicMethodsOnTask", Justification = "This is a known safe usage of Task.Result, since it only occurs when we know the task's state to be completed.")]
        internal static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> tcs, Task source)
        {
            if (source.Status == TaskStatus.Canceled)
            {
                return tcs.TrySetCanceled();
            }

            if (source.Status == TaskStatus.Faulted)
            {
                return tcs.TrySetException(source.Exception.InnerExceptions);
            }

            if (source.Status == TaskStatus.RanToCompletion)
            {
                Task<TResult> taskOfResult = source as Task<TResult>;
                return tcs.TrySetResult(taskOfResult == null ? default(TResult) : taskOfResult.Result);
            }

            return false;
        }

        /// <summary>
        /// Set a completion source from the given Task. If the task ran to completion and the result type doesn't match
        /// the type of the completion source, then a default value will be used. This is useful for converting Task into
        /// Task{AsyncVoid}, but it can also accidentally be used to introduce data loss (by passing the wrong
        /// task type), so please execute this method with care.
        /// </summary>
        /// <typeparam name="TResult">result type for completion source.</typeparam>
        /// <param name="tcs">completion source to set</param>
        /// <param name="source">Task to get values from.</param>
        /// <returns>true if this successfully sets the completion source.</returns>
        [SuppressMessage("Microsoft.WebAPI", "CR4001:DoNotCallProblematicMethodsOnTask", Justification = "This is a known safe usage of Task.Result, since it only occurs when we know the task's state to be completed.")]
        internal static bool TrySetFromTask<TResult>(this TaskCompletionSource<Task<TResult>> tcs, Task source)
        {
            if (source.Status == TaskStatus.Canceled)
            {
                return tcs.TrySetCanceled();
            }

            if (source.Status == TaskStatus.Faulted)
            {
                return tcs.TrySetException(source.Exception.InnerExceptions);
            }

            if (source.Status == TaskStatus.RanToCompletion)
            {
                // Sometimes the source task is Task<Task<TResult>>, and sometimes it's Task<TResult>.
                // The latter usually happens when we're in the middle of a sync-block postback where
                // the continuation is a function which returns Task<TResult> rather than just TResult,
                // but the originating task was itself just Task<TResult>. An example of this can be
                // found in TaskExtensions.CatchImpl().
                Task<Task<TResult>> taskOfTaskOfResult = source as Task<Task<TResult>>;
                if (taskOfTaskOfResult != null)
                {
                    return tcs.TrySetResult(taskOfTaskOfResult.Result);
                }

                Task<TResult> taskOfResult = source as Task<TResult>;
                if (taskOfResult != null)
                {
                    return tcs.TrySetResult(taskOfResult);
                }

                return tcs.TrySetResult(TaskHelpers.FromResult(default(TResult)));
            }

            return false;
        }

        /// <summary>
        /// Used as the T in a "conversion" of a Task into a Task{T}
        /// </summary>
        private struct AsyncVoid
        {
        }

        /// <summary>
        /// This class is a convenient cache for per-type cancelled tasks
        /// </summary>
        private static class CancelCache<TResult>
        {
            public static readonly Task<TResult> Canceled = GetCancelledTask();

            private static Task<TResult> GetCancelledTask()
            {
                TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
                tcs.SetCanceled();
                return tcs.Task;
            }
        }

        // These classes are the engine that implements Iterate and Iterate<T>
        private static class IterateEngine
        {
            public static Task Run(IEnumerable<Task> iterator, CancellationToken cancellationToken)
            {
                // WARNING: This code uses LINQ Select to ensure that we get deferred execution (i.e., we
                // don't start running all the tasks all at once). If you touch this code, please ensure
                // that this behavior is preserved.
                return IterateEngine<AsyncVoid>.Run(iterator.Select(t => t.ToTask<AsyncVoid>()), cancellationToken);
            }
        }

        private class IterateEngine<TResult>
        {
            private CancellationToken _cancellationToken;
            private TaskCompletionSource<IEnumerable<TResult>> _completionSource;
            private IEnumerator<Task<TResult>> _enumerator;
            private List<TResult> _results;
            private SynchronizationContext _syncContext;

            public static Task<IEnumerable<TResult>> Run(IEnumerable<Task<TResult>> iterator, CancellationToken cancellationToken)
            {
                IterateEngine<TResult> engine = new IterateEngine<TResult>
                {
                    _cancellationToken = cancellationToken,
                    _completionSource = new TaskCompletionSource<IEnumerable<TResult>>(),
                    _enumerator = iterator.GetEnumerator(),
                    _results = new List<TResult>(),
                    _syncContext = SynchronizationContext.Current
                };

                RunNext(engine);
                return engine._completionSource.Task.Finally(engine._enumerator.Dispose);
            }

            private static void RunNext(IterateEngine<TResult> engine)
            {
                if (engine._syncContext != null && engine._syncContext != SynchronizationContext.Current)
                {
                    engine._syncContext.Post(RunNextCallback, engine);
                }
                else
                {
                    RunNextCallback(engine);
                }
            }

            // TODO: This class can become more efficient once we take a hard 4.5 dependency. In 4.0, ContinueWith
            // does not offer you the ability to pass a state object; once it does, we can change the implementation
            // of RunNextCallback to remove the closure around "engine".
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The caught exception type is reflected into a faulted task.")]
            [SuppressMessage("Microsoft.WebAPI", "CR4001:DoNotCallProblematicMethodsOnTask", Justification = "This usage is known to be safe.")]
            private static void RunNextCallback(object state)
            {
                IterateEngine<TResult> engine = (IterateEngine<TResult>)state;

                try
                {
                    if (engine._cancellationToken.IsCancellationRequested)
                    {
                        engine._completionSource.TrySetCanceled();
                    }
                    else if (engine._enumerator.MoveNext())
                    {
                        engine._enumerator.Current.ContinueWith(previous =>
                        {
                            switch (previous.Status)
                            {
                                case TaskStatus.Faulted:
                                case TaskStatus.Canceled:
                                    engine._completionSource.TrySetFromTask(previous);
                                    break;

                                default:
                                    engine._results.Add(previous.Result);
                                    RunNext(engine);
                                    break;
                            }
                        });
                    }
                    else
                    {
                        engine._completionSource.TrySetResult(engine._results);
                    }
                }
                catch (Exception e)
                {
                    engine._completionSource.TrySetException(e);
                }
            }
        }
    }
}
