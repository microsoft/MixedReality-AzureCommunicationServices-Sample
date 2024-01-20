// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CustomAudioReceiver : MonoBehaviour
{
    private const int unityAudioFormatSizeInBytyes = sizeof(float);

    private AudioClip audioClip = null;
    private AudioSource audioSource = null;
    private CustomAudioReceiverSettings settings = default;
    private CircularQueue<NativeBuffer> samplesQueue = null;
    private ConcurrentQueue<Action> updateActions = new ConcurrentQueue<Action>();
    private int inputLengthInBytes = 0;

    [SerializeField]
    [Tooltip("The maximum length of samples to save in buffer")]
    private int maxBufferSizeInSeconds = 5;

    public bool IsProcessing { get; private set; } = false;

    /// <summary>
    /// Initialize behaviour
    /// </summary>
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Force audio generation to stop on being destroyed.
    /// </summary>
    private void OnDestroy()
    {
        OnDestoryed();
        StopReceiving();
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
    /// Any time diabled clear samples. This is mainly for debugging purposes.
    /// </summary>
    private void OnDisable()
    {
        samplesQueue?.Clear();
    }

    /// <summary>
    /// Beging receiving audio
    /// </summary>
    /// <param name="settings"></param>
    public void Process(CustomAudioReceiverSettings settings)
    {
        QueueUpdateAction(() => StartProcessing(settings));
    }

    /// <summary>
    /// Stop receiving audio
    /// </summary>
    public void Stop()
    {
        QueueUpdateAction(() => StopReceiving());
    }

    /// <summary>
    /// Add samples to process
    /// </summary>
    public void AddSamples(NativeBuffer samples)
    {

        if (!IsProcessing || samples == null)
        {
            return;
        }

        // initialize the unity audio source with known buffer size
        // and don't queue samples until audio source is ready.
        if (audioClip == null)
        {
            QueueUpdateAction(() => StartPlaying(samples.Length));
            return;
        }

        samplesQueue?.Enqueue(samples);
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
    /// Queue an action to run on the main Unity loop.
    /// </summary>
    protected void QueueUpdateAction(Action action)
    {
        updateActions.Enqueue(action);
    }

    /// <summary>
    /// Start receiving audio
    /// </summary>
    private void StartProcessing(CustomAudioReceiverSettings recevierSettings)
    {
        if (IsProcessing || audioSource == null)
        {
            return;
        }

        StopPlaying();
        settings = recevierSettings;
        IsProcessing = true;
    }

    /// <summary>
    /// Starting playing audio clip
    /// </summary>
    private void StartPlaying(int inputLengthInBytes)
    {
        if (audioClip == null)
        {
            this.inputLengthInBytes = 
                inputLengthInBytes;

            int outputLenghtOfSamples =
                (inputLengthInBytes) /
                (unityAudioFormatSizeInBytyes);

            audioClip = AudioClip.Create(
                "CustomAudioReceiver",
                outputLenghtOfSamples,
                settings.Channels,
                settings.SampleRate,
                true,
                OnAudioRead);

            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }


    /// <summary>
    /// Stop playing audio player. Get ready for new stream.
    /// </summary>
    private void StopPlaying()
    {
        audioClip = null;
        audioSource.Stop();
        samplesQueue?.Clear();
    }

    /// <summary>
    /// Stop receiving audio
    /// </summary>
    private void StopReceiving()
    {
        IsProcessing = false;
        StopPlaying();
    }

    bool temp = false;
    private void OnAudioRead(float[] destination)
    {
        // Don't start caching input samples until unity player has started playing
        if (samplesQueue == null)
        {
            temp = true;
            samplesQueue = new CircularQueue<NativeBuffer>((settings.SampleRate * maxBufferSizeInSeconds) / inputLengthInBytes);
        }

        // Clear all but the latest sample if queue is full
        if (samplesQueue.IsFull)
        {
            samplesQueue.ClearAllButLast();
        }

        int writeAt = 0;
        if (samplesQueue.TryDequeue(out NativeBuffer buffer))
        {
            int readAt = 0;
            short[] pcm16BitAudio = buffer.ToShortArray();
            Fill(pcm16BitAudio, destination, ref readAt, ref writeAt);
            Debug.Assert(readAt == pcm16BitAudio.Length);
        }

        // Fill remaining buffer with silience. This is mainly used when the samples queue is empty,
        // but also helps in the unlikely case the input buffer size doesn't align with output size
        if (writeAt < destination.Length)
        {
            // If not starting at index zero, there is something wrong with the buffer sizes.
            Debug.Assert(writeAt == 0);

            Array.Fill(destination, 0, writeAt, destination.Length - writeAt);
        }
    }

    /// <summary>
    /// Convert to signed value.
    /// </summary>
    private int ToSignedLength(uint length)
    {
        int signed = (int)length;
        if (signed < 0)
        {
            throw new OverflowException();
        }
        return signed;
    }

    /// <summary>
    /// Convert 16 bit PCM data to float PCM data.
    /// </summary>
    private void Fill(short[] source, float[] destination, ref int readAt, ref int writeAt)
    {
        for (; readAt < source.Length && writeAt < destination.Length; readAt++, writeAt++)
        {
            short sample = source[readAt];
            float sample_float = (float)sample / short.MaxValue;
            destination[writeAt] = Mathf.Clamp(sample_float, -1.0f, 1.0f);
        }
    }
}

public struct CustomAudioReceiverSettings
{
    public int Channels;
    public int SampleRate;
}
