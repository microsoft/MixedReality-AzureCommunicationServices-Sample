// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine.Events;

/// <summary>
/// A Unity event that sends an object.
/// </summary>
[Serializable]
public class ObjectChangeEvent : UnityEvent<object>
{
}
