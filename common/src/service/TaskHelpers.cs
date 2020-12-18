// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#if NET451
using System.Transactions;
#endif

namespace Microsoft.Azure.Devices.Common
{
    internal static class TaskHelpers
    {
        public static readonly Task CompletedTask = Task.FromResult(default(VoidTaskResult));

        /// <summary>
        /// Create a Task based on Begin/End IAsyncResult pattern.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="state">
        /// This parameter helps reduce allocations by passing state to the Funcs. e.g.:
        ///  await TaskHelpers.CreateTask(
        ///      (c, s) => ((Transaction)s).BeginCommit(c, s),
        ///      (a) => ((Transaction)a.AsyncState).EndCommit(a),
        ///      transaction);
        /// </param>
        public static void Fork(this Task thisTask)
        {
            Fork(thisTask, "TaskExtensions.Fork");
        }

        public static void Fork(this Task thisTask, string tracingInfo)
        {
            Fx.Assert(thisTask != null, "task is required!");
            thisTask.ContinueWith(t => Fx.Exception.TraceHandled(t.Exception, tracingInfo), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        public static IAsyncResult ToAsyncResult(this Task task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }

                    if (callback != null)
                    {
                        callback(tcs.Task);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        public static IAsyncResult ToAsyncResult<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(task.Result);
                    }

                    if (callback != null)
                    {
                        callback(tcs.Task);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        internal struct VoidTaskResult
        {
        }
    }
}
