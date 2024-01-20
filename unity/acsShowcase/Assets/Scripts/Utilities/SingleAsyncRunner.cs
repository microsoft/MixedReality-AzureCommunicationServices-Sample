// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Run a single async operation at a time.
/// </summary>
public class SingleAsyncRunner : MonoBehaviour
{
    private bool asyncInProgress = false;
    private bool destroyed = false;
    private ConcurrentQueue<Func<Task>> asyncOperations = new ConcurrentQueue<Func<Task>>();

    private void Update()
    {
        RunAsyncWorker();
    }

    private void OnDestroy()
    {
        destroyed = true;
        RunAsyncWorker();
    }

    /// <summary>
    /// Attempt to run async operation only if there isn't a currently running operation.
    /// This function is not thread safe.
    /// </summary>
    /// <returns>
    /// True if action will be executed, false otherwise.
    /// </returns>
    public void QueueAsync(Func<Task> action)
    {
        asyncOperations.Enqueue(action);

        // If GameOjbect is destroyed, run this async operation now. Otherwise it may not run.
        if (destroyed)
        {
            RunAsyncWorker();
        }
    }

    private async void RunAsyncWorker()
    { 
        if (asyncInProgress)
        {
            return;
        }

        asyncInProgress = true;
        try
        {
            while (asyncOperations.TryDequeue(out var action))
            {
                await action();
            }
        }
        finally
        {
            asyncInProgress = false;
        }
    }
}
