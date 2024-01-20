// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureNotSupportedManager : MonoBehaviour
{

    [SerializeField] private GameObject featureNotEnabledObject;
    [SerializeField] private GameObject confirmSignOutObject;

    private void Awake()
    {
        UserObject.OnSelectedParticipantCall += OnSelectedParticipantFeatureNotEnabled;
    }
    private void OnDestroy()
    {
        UserObject.OnSelectedParticipantCall -= OnSelectedParticipantFeatureNotEnabled;
    }

    private void OnSelectedParticipantFeatureNotEnabled()
    {
        featureNotEnabledObject.SetActive(true);
        confirmSignOutObject.SetActive(false);
    }
}
