﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SfB.PlatformService.SDK.Common
{
    /// <summary>
    /// Emptry structure.
    /// </summary>
    internal struct Empty
    {
    }

    public static class TaskHelpers
    {
        /// <summary>
        /// Cached completed task.
        /// </summary>
        private static Task s_completedTask = TaskHelpers.FromResult<Empty>(default(Empty));

        static TaskHelpers()
        {
            TaskCompletionSource<Empty> tcs = new TaskCompletionSource<Empty>();
            tcs.TrySetResult(new Empty());
            s_completedTask = tcs.Task;
        }

        public static Task CompletedTask
        {
            get { return s_completedTask; }
        }

        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.TrySetResult(result);
            return tcs.Task;
        }
    }

    public static class TaskExtensions
    {
        public static async Task TimeoutAfterAsync(this Task task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);

            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }

        public static async Task<T> TimeoutAfterAsync<T>(this Task<T> task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);

            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }

        /// <summary>
        /// Observes platform exception and then ignores it.
        /// </summary>
        /// <param name="task">Task to set exception handler.</param>
        /// <returns>Returns the task after setting the exception handler.</returns>
        public static Task Observe<TException>(this Task task)
            where TException : Exception
        {
            return task.FinishWith((exception) => (exception is TException));
        }

        /// <summary>
        /// Sets exception handler for a particular task.
        /// </summary>
        /// <param name="task">Task to set exception handler.</param>
        /// <param name="exceptionHandler">Exception handler Cannot be null.</param>
        /// <returns>Returns the task after setting the exception handler.</returns>
        private static Task FinishWith(this Task task, Func<Exception, bool> exceptionHandler)
        {
            task.ContinueWith(
                pTask =>
                {
                    Exception exceptionToHandle = pTask.Exception.GetBaseException();
                    Logger.Instance.Error(exceptionToHandle,
                          "Task completed with Exception"
                         );

                    bool exceptionHandled = false;
                    try
                    {
                        exceptionHandled = exceptionHandler(exceptionToHandle);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex,
                                  "Exception happen in handling Exception"
                                 );
                    }
                    finally
                    {
                        if (!exceptionHandled)
                        {
                            Logger.Instance.Error(
                                  "Exception was not handled by the exception handler in finish with.");
                        }
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }
    }
}
