// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine.Events;

/// <summary>
/// A Unity event that sends a string.
/// </summary>
[Serializable]
public class StringChangeEvent : UnityEvent<string>
{
}
