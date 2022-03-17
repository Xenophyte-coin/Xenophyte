﻿using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager
{
    public class TaskManager
    {
        private static bool TaskManagerEnabled;
        private static CancellationTokenSource _cancelTaskManager = new CancellationTokenSource();
        private static List<ClassTaskObject> _taskCollection = new List<ClassTaskObject>();
        private const int MaxTaskClean = 10000;
        public static long CurrentTimestampMillisecond { get; private set; }

        /// <summary>
        /// Enable the task manager. Check tasks, dispose them if they are faulted, completed, cancelled, or if a timestamp of end has been set and has been reached.
        /// </summary>
        public static void EnableTaskManager()
        {
            TaskManagerEnabled = true;

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    using (DisposableList<int> listTaskToRemove = new DisposableList<int>())
                    {
                        while (TaskManagerEnabled)
                        {

                            for (int i = 0; i < _taskCollection.Count; i++)
                            {
                                if (!_taskCollection[i].Disposed && _taskCollection[i].Started && _taskCollection[i].Task != null)
                                {
                                    bool doDispose = false;

                                    if (_taskCollection[i].Task != null && (
#if NET5_0_OR_GREATER
                                        _taskCollection[i].Task.IsCompletedSuccessfully ||
#endif
                                        _taskCollection[i].Task.IsCanceled || _taskCollection[i].Task.IsFaulted))
                                        doDispose = true;
                                    else
                                    {
                                        if (_taskCollection[i].TimestampEnd > 0 && _taskCollection[i].TimestampEnd < CurrentTimestampMillisecond)

                                        {
                                            doDispose = true;
                                            try
                                            {
                                                if (_taskCollection[i].Cancellation != null)
                                                {
                                                    if (!_taskCollection[i].Cancellation.IsCancellationRequested)
                                                        _taskCollection[i].Cancellation.Cancel();
                                                }
                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }
                                        }
                                    }

                                    if (doDispose)
                                    {
                                        _taskCollection[i].Disposed = true;
                                        ClassUtility.CloseSocket(_taskCollection[i].Socket);
                                        _taskCollection[i].Task?.Dispose();
                                        listTaskToRemove.Add(i);
                                    }
                                }

                                if (listTaskToRemove.Count >= MaxTaskClean)
                                {
                                    foreach (int taskId in listTaskToRemove.GetList)
                                        _taskCollection.RemoveAt(taskId);

                                    _taskCollection.TrimExcess();
                                    listTaskToRemove.Clear();
                                }
                            }
                            await Task.Delay(1000);
                        }

                    }

                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                Task.Factory.StartNew(async () =>
                {
                    while(TaskManagerEnabled)
                    {
                        CurrentTimestampMillisecond = ClassUtility.GetCurrentTimestampInMillisecond();

                        for (int i = 0; i < _taskCollection.Count; i++)
                        {
                            if (i > _taskCollection.Count)
                                break;

                            if (!_taskCollection[i].Started)
                            {

                                try
                                {
                                    _taskCollection[i].Task = Task.Run(_taskCollection[i].Action, _taskCollection[i].Cancellation.Token);
                                    await _taskCollection[i].Task.ConfigureAwait(false);
                                    
                                }
                                catch
                                {
                                    // Catch the exception if the task cannot start.
                                }
                                _taskCollection[i].Started = true;

                            }

                        }
                        await Task.Delay(10);
                    }
                }, _cancelTaskManager.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Insert task to manager.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="timestampEnd"></param>
        /// <param name="cancellation"></param>
        public static void InsertTask(Action action, long timestampEnd, CancellationTokenSource cancellation, Socket socket = null)
        {


            if (TaskManagerEnabled)
            {

                CancellationTokenSource cancellationTask = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellation != null ?
                       cancellation.Token : new CancellationToken(),
                       timestampEnd > 0 ? new CancellationTokenSource((int)(timestampEnd - CurrentTimestampMillisecond)).Token : new CancellationToken());

                _taskCollection.Add(new ClassTaskObject(action, cancellationTask, timestampEnd, socket));
            }


        }

        /// <summary>
        /// Stop task manager.
        /// </summary>
        public static void StopTaskManager()
        {
            TaskManagerEnabled = false;
            if (!_cancelTaskManager.IsCancellationRequested)
                _cancelTaskManager.Cancel();
        }
    }
}
