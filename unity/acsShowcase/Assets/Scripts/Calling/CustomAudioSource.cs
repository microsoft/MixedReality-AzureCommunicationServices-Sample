// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using UnityEngine;

public abstract class CustomAudioSource : MonoBehaviour
{
    private ConcurrentQueue<Action> updateActions = new ConcurrentQueue<Action>();

    /// <summary>
    /// Get if source is currently capturing
    /// </summary>
    public abstract bool IsCapturing { get; }

    /// <summary>
    /// Event fired when an audio sample is ready to be consumed.
    /// </summary>
    public event MediaSampleEvent OnMediaSamplesReady;

    /// <summary>
    /// Force audio generation to stop on being destroyed.
    /// </summary>
    private void OnDestroy()
    {
        OnDestoryed();
        StopGenerating();
    }

    /// <summary>
    /// Process queued actions
    /// </summary>
    private void Update()
    {
        OnUpdate();
        while (updateActions.TryDequeue(out var action))
        {
            action();
        }
    }

    /// <summary>
    /// Start generating samples.
    /// </summary>
    public void Generate(CustomAudioSourceSettings settings)
    {
        QueueUpdateAction(() => StartGenerating(settings));
    }

    /// <summary>
    /// Stop generating samples.
    /// </summary>
    public void Stop()
    {
        QueueUpdateAction(() => StopGenerating());
    }

    /// <summary>
    /// Handle being destroyed
    /// </summary>
    protected virtual void OnDestoryed()
    {
    }

    /// <summary>
    /// Handle being updated
    /// </summary>
    protected virtual void OnUpdate()
    {
    }

    /// <summary>
    /// Start generating samples.
    /// </summary>
    protected abstract void StartGenerating(CustomAudioSourceSettings settings);

    /// <summary>
    /// Stop generating samples.
    /// </summary>
    protected abstract void StopGenerating();

    /// <summary>
    /// Queue an action to run on the main Unity loop.
    /// </summary>
    protected void QueueUpdateAction(Action action)
    {
        updateActions.Enqueue(action);
    }

    protected void FireMediaSamplesReady(MediaSampleArgs args)
    {
        OnMediaSamplesReady?.Invoke(this, args);
    }
}

public struct CustomAudioSourceSettings
{
    public int Channels;
    public int SampleRate;
    public int SampleSizeInBytes;
    public long ExpectedBufferSizeInBytes;
}
