// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// This enum matches the names and values found in Microsoft.Identity.Client.AadAuthorityAudience.
    /// </summary>
    public enum AppAuthorityAudience
    {
        //
        // Summary:
        //     The sign-in audience was not specified
        None = 0,
        //
        // Summary:
        //     Users with a Microsoft work or school account in my organization�s Azure AD tenant
        //     (i.e. single tenant). Maps to https://[instance]/[tenantId]
        AzureAdMyOrg = 1,
        //
        // Summary:
        //     Users with a personal Microsoft account, or a work or school account in any organization�s
        //     Azure AD tenant Maps to https://[instance]/common/
        AzureAdAndPersonalMicrosoftAccount = 2,
        //
        // Summary:
        //     Users with a Microsoft work or school account in any organization�s Azure AD
        //     tenant (i.e. multi-tenant). Maps to https://[instance]/organizations/
        AzureAdMultipleOrgs = 3,
        //
        // Summary:
        //     Users with a personal Microsoft account. Maps to https://[instance]/consumers/
        PersonalMicrosoftAccount = 4
    }

    public struct AuthenticationEventData
    {
        public AuthenticationEventData(Type type, AuthenticationRequest[] requests = null)
        {
            EventType = type;
            Requests = requests;
        }

        public enum Type
        {
            Unknown,
            Authenticating,
            Authenticated,
            Failure,
            Cleared,
        }

        public Type EventType { get; }

        public AuthenticationRequest[] Requests { get; }
    }

    /// <summary>
    /// Unity GraphEvent to be raised alongside <see cref="OnAuthenticationEvent"/>.
    /// </summary>
    [Serializable]
    public class AuthenticationEvent : UnityEvent<AuthenticationEventData> { };

    /// <summary>
    /// GraphEvent to be raised alongside <see cref="OnAuthenticationEvent"/>.
    /// </summary>
    public delegate void AuthenticationEventDelegate(AuthenticationEventData eventData);

    /// <summary>
    /// Basic implementation for authentication to Azure services.
    /// </summary>
    public class AuthenticationManager : MonoBehaviour
    {
        #region serialized fields

        [Header("Scoped Requests")]

        [SerializeField]
        [Tooltip("The set of scopes to request")]
        private AuthenticationRequest[] requests = new AuthenticationRequest[0];

        [Header("Authentication Settings")]

        [SerializeField]
        [Tooltip("The audience that can authenticate with this application")]
        private AppAuthorityAudience authority = AppAuthorityAudience.None;

        [SerializeField]
        [Tooltip("The Azure application or client ID.")]
        private string clientID = string.Empty;

        /// <summary>
        /// Get the Azure application or client ID.
        /// </summary>
        public string ClientID
        {
            get => clientID;
        }

        [SerializeField]
        [Tooltip("The Azure tenant id.")]
        private string tenantId = string.Empty;

        [SerializeField]
        [Tooltip("Should tenant be used")]
        private bool useTenant = false;

        [Header("General Settings")]
        
        #endregion serialized fields

        #region events
        [Header("Events")]

        /// <summary>
        /// Unity event triggers when authentication starts and ends.
        /// </summary>
        [SerializeField, Tooltip("Triggers when authentication starts and ends.")]
        public AuthenticationEvent onAuthenticationEvent = new AuthenticationEvent();

        /// <inheritdoc/>
        public event AuthenticationEventDelegate OnAuthenticationEvent;

        #endregion

        #region properties
        public AppAuthorityAudience Authority
        {
            get => authority;
            set => authority = value;
        }

        /// <summary>
        /// The Azure app or client id
        /// </summary>
        public string ClientId
        {
            get => clientID;
            set => clientID = value;
        }

        /// <summary>
        /// The Azure tenant id
        /// </summary>
        public string TenantId
        {
            get => tenantId;
            set => tenantId = value;
        }

        /// <summary>
        /// Should tenant be used
        /// </summary>
        public bool UseTenant
        {
            get => useTenant;
            set => useTenant = value;
        }

        public AuthenticationRequest[] Requests
        {
            get => requests;
            set => requests = value;
        }

        /// <inheritdoc />
        public bool IsAuthenticating { get; private set; }

        /// <inheritdoc />
        public bool UsesSigninDeviceCode
        {
            get
            {
                // Currently, only AndroidSignin uses the DeviceCode mechanism, MsalSignin throws and exception on query
                try
                {
                    var _dummy = _signin.GetDeviceCode();
                    // don't care about return value, only check whether it returns without exception
                    return true;
                }
                catch (NotImplementedException)
                {
                    return false;
                }
            }
        }

        #endregion

        #region private fields

        private ISignin _signin = null;

        #endregion

        #region methods

        /// <inheritdoc />
        public void InitializeSignIn()
        {
            if (_signin != null)
            {
                return;
            }

#if UNITY_WSA && !UNITY_EDITOR
            _signin = new WamSignin();
#else
            _signin = new MsalSignin();
#endif

            if (UseTenant)
            {
                _signin.Initialize(ClientId, string.Empty, TenantId);
            }
            else
            {
                _signin.Initialize(ClientId, Authority.ToString(), string.Empty);
            }
        }

        /// <inheritdoc />
        public string GetSigninDeviceCode()
        {
            return _signin.GetDeviceCode();
        }

        /// <inheritdoc />
        public Task<bool> ClearAuthenticationAsync()
        {
            return ClearAuthenticationAsync(requests);
        }

        /// <inheritdoc />
        public Task<bool> ClearAuthenticationAsync(AuthenticationRequest request)
        {
            return ClearAuthenticationAsync(new AuthenticationRequest[] { request });
        }

        /// <inheritdoc />
        public async Task<bool> ClearAuthenticationAsync(AuthenticationRequest[] requests)
        {
            IList<AuthenticationRequest> allRequests = FindAllRequests(requests);

            try
            {
                await _signin.ClearCacheAsync();
            }
            catch (Exception)
            {
                return false;
            }

            foreach (AuthenticationRequest request in allRequests)
            {
                request.ClearToken();
            }

            return true;
        }        

        /// <inheritdoc />
        public async Task<AuthenticationRequest[]> AuthenticateAsync(CancellationToken cancelationToken)
        {
            var requests = Requests;
            await AuthenticateAsync(requests, cancelationToken);
            return requests;
        }

        /// <inheritdoc />
        public async Task<AuthenticationRequest> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancelationToken)
        {
            await AuthenticateAsync(new AuthenticationRequest[] { request }, cancelationToken);
            return request;
        }

        /// <inheritdoc />
        public async Task<AuthenticationRequest[]> AuthenticateAsync(AuthenticationRequest[] requests, CancellationToken cancelationToken)
        {
            IsAuthenticating = true;
            OnAuthenticationEvent?.Invoke(new AuthenticationEventData(AuthenticationEventData.Type.Authenticating));

            InitializeSignIn();
            List<AuthenticationRequest> allRequests = FindAllRequests(requests);

            try
            {
                for (int i = 0; i < allRequests.Count && !cancelationToken.IsCancellationRequested; i++)
                {
                    var request = allRequests[i];
                    if (!request.TokenResponse.IsValid())
                    {
                        var scopes = request.Scopes ?? new string[0];
                        Log.Information<AuthenticationManager>("Attempting to get token from resource '{0}' with scopes '{1}'", request.Resource, scopes);
                        request.CompleteAuthentication(await _signin.GetTokenAsync(request.Scopes ?? new string[0], request.Resource, cancelationToken));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error<AuthenticationManager>("Failed to authenticate user. Exception: {0}", e);
                allRequests = null;
            }

            IsAuthenticating = false;

            if (cancelationToken.IsCancellationRequested || allRequests == null)
            {
                OnAuthenticationEvent?.Invoke(new AuthenticationEventData(AuthenticationEventData.Type.Failure, null));
                return null;
            }
            else
            {
                OnAuthenticationEvent?.Invoke(new AuthenticationEventData(AuthenticationEventData.Type.Authenticated, requests));
                return allRequests.ToArray();
            }
        }

        #endregion

        #region MonoBehaviour methods

        private void Awake()
        {
            InitializeSignIn();
            OnAuthenticationEvent += InvokeAuthenticationUnityEvent;
        }

        #endregion

        #region private methods
        private List<AuthenticationRequest> FindAllRequests(AuthenticationRequest[] additionalRequests = null)
        {
            List<AuthenticationRequest> allRequests = new List<AuthenticationRequest>();

            if (Requests != null)
            {
                foreach (var request in Requests)
                {
                    if (request != null)
                    {
                        allRequests.Add(request);
                    }
                }
            }

            if (additionalRequests != null)
            {
                foreach (var request in additionalRequests)
                {
                    if (request != null && !allRequests.Contains(request))
                    {
                        allRequests.Add(request);
                    }
                }
            }

            return allRequests;
        }

        private void InvokeAuthenticationUnityEvent(AuthenticationEventData eventData)
        {
            onAuthenticationEvent.Invoke(eventData);
        }

        #endregion
    }
}
