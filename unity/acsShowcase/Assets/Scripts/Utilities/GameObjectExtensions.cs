// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Extensions to Unity's GameObject class.
/// </summary>
public static class GameObjectExtensions
{
    public static T EnsureComponent<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null) 
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }    
}
