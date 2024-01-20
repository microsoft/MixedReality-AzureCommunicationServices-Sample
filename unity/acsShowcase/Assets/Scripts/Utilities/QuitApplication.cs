// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

/// <summary>
/// A class that handles UI requests to quit the application.
/// </summary>
public class QuitApplication : MonoBehaviour
{
    public void Quit()
    {
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            Application.Quit();
        }
    }
}
