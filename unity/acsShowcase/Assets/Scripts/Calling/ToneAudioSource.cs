// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;
using System.ComponentModel;
using System.Threading;
using UnityEngine;

public class ToneAudioSource : CustomAudioSource
{
    private BackgroundWorker backgroundWorker = null;
    private System.Random random = new System.Random();

    public override bool IsCapturing => backgroundWorker != null;

    protected override void StartGenerating(CustomAudioSourceSettings settings)
    {
        if (backgroundWorker != null)
        {
            return;
        }

        backgroundWorker = new BackgroundWorker();
        backgroundWorker.WorkerSupportsCancellation = true;
        backgroundWorker.WorkerReportsProgress = false;
        backgroundWorker.DoWork += GenerateSamples;
        backgroundWorker.RunWorkerAsync(settings);
    }

    protected override void StopGenerating()
    {
        if (backgroundWorker != null)
        {
            backgroundWorker.CancelAsync();
            backgroundWorker.DoWork -= GenerateSamples;
            backgroundWorker = null;
        }
    }

    /// <summary>
    /// Send samples to listener, which will likely encode frame into an audio stream.
    /// </summary>
    private void GenerateSamples(object sender, DoWorkEventArgs e)
    {
        bool run = true;
        BackgroundWorker worker = (BackgroundWorker)sender;
        CustomAudioSourceSettings settings = (CustomAudioSourceSettings)e.Argument;

        int sampleBufferSize = (int)settings.ExpectedBufferSizeInBytes;
        if (sampleBufferSize < 0)
        {
            Debug.LogError("Can't generate audio, expected size is too big.");
        }

        NativeBuffer nativeBuffer = new NativeBuffer(sampleBufferSize);
        nativeBuffer.GetData(out IntPtr sampleBufferPointer, out sampleBufferSize);

        RawAudioBuffer outgoingAudioBuffer = new RawAudioBuffer()
        {
            Buffer = nativeBuffer
        };

        long samplesToGenerate = settings.ExpectedBufferSizeInBytes / (settings.Channels * settings.SampleSizeInBytes);
        double sampleDuration = (double)samplesToGenerate / settings.SampleRate;
        TimeSpan outgoingAudioDuration = TimeSpan.FromSeconds(sampleDuration);
        long timeStampInTicks = DateTimeOffset.UtcNow.Ticks;

        long currentSampleNumber = 0;

        while (run && !worker.CancellationPending)
        {
            currentSampleNumber = GenerateToneData(
                sampleBufferPointer,
                sampleBufferSize / settings.SampleSizeInBytes,
                currentSampleNumber,
                samplesToGenerate: samplesToGenerate,
                frequency: 1000,
                settings.Channels,
                settings.SampleRate);

            try
            {
                FireMediaSamplesReady(new MediaSampleArgs() { 
                    Buffer = nativeBuffer,
                    Container = outgoingAudioBuffer
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send audio samples. Exception: {ex}");
                run = false;
            }

            outgoingAudioBuffer.TimestampInTicks = timeStampInTicks;
            timeStampInTicks += outgoingAudioDuration.Ticks;

            Thread.Sleep(outgoingAudioDuration);
        }
    }

    /// <summary>
    /// Write samples to make a tone at the specifies frquency.
    /// </summary>
    /// <remarks>
    /// Currenlt yonly handle PCM 16
    /// </remarks>
    unsafe private long GenerateToneData(
        IntPtr buffer,
        int bufferSize,
        long currentSampleNumber,
        long samplesToGenerate,
        int frequency,
        int channelCount,
        int samplesPerSecond)
    {
        int counter = 0;
        float referenceSoundPressure = 20.0f;   // dB
        float referenceAudioToneLevel = -20.0f; // dB
        var sampleBuffer = (short*)buffer;

        int audioMaxLevel = short.MaxValue;
        float maxLevel = audioMaxLevel * Mathf.Pow(10.0f, referenceAudioToneLevel / referenceSoundPressure);
        maxLevel = audioMaxLevel;

        var endSampleNumber = currentSampleNumber + samplesToGenerate;
        for (; currentSampleNumber < endSampleNumber; ++currentSampleNumber)
        {
            float timeInFractionalSeconds = (float)currentSampleNumber / samplesPerSecond;
            float amplitude = Mathf.Sin(2 * Mathf.PI * frequency * timeInFractionalSeconds);

            // casting NaN to int is undefined behavior
            if (double.IsNaN(amplitude))
            {
                amplitude = 0;
            }

            var noise = (float)random.NextDouble() * 0.2f - 0.1f;
            amplitude = Mathf.Max(Mathf.Min(amplitude + noise, 1.0f), -1.0f);
            var sample = (short)(maxLevel * amplitude);

            for (var channel = 0; channel != channelCount; ++channel)
            {
                if (counter >= bufferSize)
                {
                    throw new IndexOutOfRangeException();
                }

                sampleBuffer[counter++] = sample;
            }
        }

        return currentSampleNumber;
    }
}
