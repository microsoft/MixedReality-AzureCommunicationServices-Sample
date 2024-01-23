// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine.Events;

/// <summary>
/// A Unity event that sends a booleans.
/// </summary>
[Serializable]
public class BooleanChangedEvent : UnityEvent<bool>
{
}
