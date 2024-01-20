// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;

public static class AudioExtensions
{
    public static int ToInteger(this AudioStreamChannelMode value)
    {
        switch (value)
        {
            case AudioStreamChannelMode.Mono:
                return 1;

            case AudioStreamChannelMode.Stereo:
                return 2;

            default:
                throw new Exception($"Unknown audio channel mode '{value}'");
        }
    }

    public static int ToInteger(this AudioStreamSampleRate value)
    {
        switch (value)
        {
            case AudioStreamSampleRate.Hz_16000:
                return 16000;

            case AudioStreamSampleRate.Hz_22050:
                return 22050;

            case AudioStreamSampleRate.Hz_24000:
                return 24000;

            case AudioStreamSampleRate.Hz_32000:
                return 32000;

            case AudioStreamSampleRate.Hz_44100:
                return 44100;

            case AudioStreamSampleRate.Hz_48000:
                return 48000;

            default:
                throw new Exception($"Unknown audio sample rate '{value}'");
        }
    }

    public static int ToMilliseconds(this AudioStreamBufferDuration value)
    {
        switch (value)
        {
            case AudioStreamBufferDuration.Ms10:
                return 10;

            case AudioStreamBufferDuration.Ms20:
                return 20;

            default:
                throw new Exception($"Unknown OutgoingAudioMsOfDataPerBlock '{value}'");
        }
    }

    public static int ToSizeInBytes(this AudioStreamFormat value)
    {
        switch (value)
        {
            case AudioStreamFormat.Pcm16Bit:
                return 2;

            default:
                throw new Exception($"Unknown AudioStreamFormat '{value}'");
        }
    }
}
