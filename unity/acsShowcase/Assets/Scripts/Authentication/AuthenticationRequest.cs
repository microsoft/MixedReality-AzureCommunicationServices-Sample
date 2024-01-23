// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Azure.Communication.Calling.Unity
{
    public class AuthenticationRequest : MonoBehaviour, IAuthenticationRequest
    {
        [SerializeField]
        [Tooltip("An app specfic name of the request")]
        private string name;

        public string Name
        {
            get => name;
            set => name = value;
        }

        [SerializeField]
        [Tooltip("The scopes to request.")]
        private string[] scopes;

        public string[] Scopes
        {
            get => scopes;
            set => scopes = value;
        }

        [SerializeField]
        [Tooltip("The resource holding the scopes.")]
        private string resource;

        public string Resource
        {
            get => resource;
            set => resource = value;
        }

        [Header("Events")]

        /// <summary>
        /// Unity event triggers when authentication starts and ends.
        /// </summary>
        [SerializeField, Tooltip("Triggers when authentication starts and ends.")]
        public AuthenticationEvent onAuthenticationEvent = new AuthenticationEvent();

        /// <inheritdoc/>
        public event AuthenticationEventDelegate OnAuthenticationEvent;

        /// <inheritdoc/>
        public TokenResponse TokenResponse { get; private set; }

        #region MonoBehaviour methods

        protected void Awake()
        {
            OnAuthenticationEvent += InvokeAuthenticationUnityEvent;
        }

        #endregion

        #region public methods
        public void ClearToken()
        {
            TokenResponse = new TokenResponse();
            AuthenticationEventData data = new AuthenticationEventData(AuthenticationEventData.Type.Cleared, new AuthenticationRequest[] { this });
            OnAuthenticationEvent?.Invoke(data);
        }

        public void StartAuthenication()
        {
            TokenResponse = new TokenResponse();
            AuthenticationEventData data = new AuthenticationEventData(AuthenticationEventData.Type.Authenticating, new AuthenticationRequest[] { this });
            OnAuthenticationEvent?.Invoke(data);
        }

        public void FailAutentication()
        {
            TokenResponse = new TokenResponse();
        }

        public void CompleteAuthentication(TokenResponse tokenRespone)
        {
            TokenResponse = tokenRespone;
            AuthenticationEventData data = new AuthenticationEventData(AuthenticationEventData.Type.Authenticated, new AuthenticationRequest[] { this });
            OnAuthenticationEvent?.Invoke(data);
        }
        #endregion public methods

        #region private methods

        private void InvokeAuthenticationUnityEvent(AuthenticationEventData eventData)
        {
            onAuthenticationEvent.Invoke(eventData);
        }

        #endregion

    }
}
