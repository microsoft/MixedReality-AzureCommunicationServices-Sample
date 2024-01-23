// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Extensions to Unity's AsyncOperation class.
/// </summary>
public static class AsyncOperationExtensions 
{
    public static async Task AsTask(this AsyncOperation operation)
    {
        TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
        Action<AsyncOperation> completedHandler = (AsyncOperation obj) => taskCompletionSource.TrySetResult(obj);

        operation.completed += completedHandler;
        await taskCompletionSource.Task;
        operation.completed -= completedHandler;
    }
}
