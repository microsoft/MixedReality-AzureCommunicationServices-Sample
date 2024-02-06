// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureNotSupportedManager : MonoBehaviour
{

    [SerializeField] private GameObject featureNotEnabledObject;
    [SerializeField] private GameObject confirmSignOutObject;
    private UserObject m_CurrentSelectedUserObject;
    private void Awake()
    {
        UserObject.OnSelectedParticipantCall += OnSelectedParticipantFeatureNotEnabled;
    }
    private void OnDestroy()
    {
        UserObject.OnSelectedParticipantCall -= OnSelectedParticipantFeatureNotEnabled;
    }

    private void OnSelectedParticipantFeatureNotEnabled(UserObject userObject)
    {
        featureNotEnabledObject.SetActive(true);
        confirmSignOutObject.SetActive(false);
        m_CurrentSelectedUserObject = userObject;
    }
    public void OnCloseFeatureNotEnabledDialog()
    {
        if (m_CurrentSelectedUserObject != null)
        {
            m_CurrentSelectedUserObject.DeSelect();
            m_CurrentSelectedUserObject = null;
        }
    }
}
