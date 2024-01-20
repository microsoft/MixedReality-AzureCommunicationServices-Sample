// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;

/// <summary>
/// A singleton which hosts an Azure Communication calling client. This calling client 
/// is then shared across the application.
/// </summary>
public class CallClientHost : Singleton<CallClientHost>
{
    public CallClient CallClient { get; private set; }

    protected override void Created()
    {
        DontDestroyOnLoad(gameObject);
        CallClient = new CallClient();
    }

    protected override void Destroyed()
    {
        if (CallClient != null)
        {
            CallClient.Dispose();
            CallClient = null;
        }
    }
}
