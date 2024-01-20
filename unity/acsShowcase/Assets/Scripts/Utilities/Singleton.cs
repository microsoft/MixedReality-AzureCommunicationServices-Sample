// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

/// <summary>
/// Represents a Unity behaviour that can only have only once instance per application.
/// </summary>
public abstract class Singleton<T>: MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            Created();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        Destroyed();
    }

    private void OnDestroy()
    {
        Instance = null;
        Destroyed();
    }

    protected abstract void Created();

    protected abstract void Destroyed();
}
