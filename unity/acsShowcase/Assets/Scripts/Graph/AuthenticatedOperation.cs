// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public abstract class AuthenticatedOperation : MonoBehaviour
    {
        IAuthenticationRequest _authenticationRequest = null;
        bool _isAuthenticated = false;

        #region Serializable Fields
        [Header("Authentication")]

        [SerializeField]
        [Tooltip("The authentication manager that holds the application (client) ID. The application (client) ID is needed to set the user's presence.")]
        private AuthenticationManager authenticationManager;

        [SerializeField]
        [Tooltip("The request that holds the access token needed for this operation.")]
        private AuthenticationRequest authenticationRequest;

        [Header("Events")]

        [SerializeField]
        [Tooltip("Event raised when authenticated state changes")]
        private IsAuthenticatedChangedEvent isAuthenticatedChanged = new IsAuthenticatedChangedEvent();

        public Action<AuthenticatedOperation, bool> IsAuthenticationChanged;
        #endregion Serializable Fields

        #region Public Properties
        /// <summary>
        /// Get the application (client) ID.
        /// </summary>
        public string ClientId => authenticationManager?.ClientId;

        public IAuthenticationRequest AuthenticationRequest
        {
            get => _authenticationRequest ?? authenticationRequest;

            set
            { 
                _authenticationRequest = value; 
                if (value is AuthenticationRequest)
                {
                    authenticationRequest = (AuthenticationRequest)value;
                }
            }
        }

        public string Token
        {
            get => AuthenticationRequest?.TokenResponse.Token;
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;

            private set
            {
                if (value != _isAuthenticated)
                {
                    _isAuthenticated = value;
                    RaiseIsAuthenticatedChanged(value);
                }
            }
        }
        #endregion Public Properties

        #region MonoBehaviour Functions
        protected virtual void Start()
        {
            UpdateAccess(AuthenticationRequest);

            // value starts false. If still false raise event so UI can update
            if (!IsAuthenticated)
            {
                RaiseIsAuthenticatedChanged(IsAuthenticated);
            }
        }

        protected virtual void OnDestroy()
        {
            UpdateAccess(accressRequest: null);
        }
        #endregion MonoBehavior Functions

        #region Public Functions
        #endregion Public Function

        #region Protected Functions
        protected abstract void OnAuthenticated();
        #endregion Protected Functions

        #region Private Functions
        private void UpdateAccess(IAuthenticationRequest accressRequest)
        {
            if (_authenticationRequest != null)
            {
                _authenticationRequest.OnAuthenticationEvent -= OnAutheticationEvent;
            }

            _authenticationRequest = accressRequest;

            if (_authenticationRequest != null)
            {
                _authenticationRequest.OnAuthenticationEvent += OnAutheticationEvent;
            }

            UpdateIsAuthenticated();
        }

        private void OnAutheticationEvent(AuthenticationEventData eventData)
        {
            if (eventData.EventType == AuthenticationEventData.Type.Authenticated ||
                eventData.EventType == AuthenticationEventData.Type.Cleared)
            {
                UpdateIsAuthenticated();
            }
        }

        private void UpdateIsAuthenticated()
        { 
            IsAuthenticated = !string.IsNullOrEmpty(Token);
            if (IsAuthenticated)
            {
                OnAuthenticated();
            }
        }

        private void RaiseIsAuthenticatedChanged(bool value)
        {
            IsAuthenticationChanged?.Invoke(this, value);
            isAuthenticatedChanged?.Invoke(value);
        }
        #endregion
    }

    [Serializable]
    public class IsAuthenticatedChangedEvent : UnityEvent<bool>        
    {
    }
}
