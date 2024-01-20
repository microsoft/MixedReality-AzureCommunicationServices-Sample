// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;

public delegate void MediaSampleEvent(object sender, MediaSampleArgs args);

public struct MediaSampleArgs
{
    public NativeBuffer Buffer;
    public object Container;
}
