// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;

namespace Azure.Communication.Calling.Unity
{
    public class UserWithActions : IUser, IUserActions, IAuthenticationRequest
    {
        private MeetingManager _meetingManager = null;
        private IAuthenticationRequest _authenticationRequest = null;

        public UserWithActions(IAuthenticationRequest authenticationRequest, MeetingManager meetingManager, IUser user)
        {
            _meetingManager = meetingManager;
            this.user = user;
            _authenticationRequest = authenticationRequest;
        }

        public IUser user { get; private set; }

        public string displayName => user?.displayName;

        public string givenName => user?.givenName;

        public string jobTitle => user?.jobTitle;

        public string mail => user?.mail;

        public string officeLocation => user?.officeLocation;

        public string surname => user?.surname;

        public string userPrincipalName => user?.userPrincipalName;

        public string id => user?.id;

        public InternalUserType internalType => user?.internalType ?? InternalUserType.Unknown;

        public TokenResponse TokenResponse => _authenticationRequest?.TokenResponse ?? default;

        public event AuthenticationEventDelegate OnAuthenticationEvent
        {
            add
            {
                if (_authenticationRequest != null)
                {
                    _authenticationRequest.OnAuthenticationEvent += value;
                }
            }

            remove
            {
                if (_authenticationRequest != null)
                {
                    _authenticationRequest.OnAuthenticationEvent -= value;
                }
            }
        }

        public void Call()
        {
            if (user == null)
            {
                Log.Error<UserWithActions>("Unable to call user, user == null ({0})", displayName);

            }
            else if (_meetingManager == null)
            {
                Log.Error<UserWithActions>("Unable to call user, meeting manager == null ({0})", displayName);
            }
            else if (user.internalType == InternalUserType.CommunicationServices)
            {
                _meetingManager.Create(new CommunicationServicesIdentity(user));
            }
            else
            {
                _meetingManager.Create(new TeamsUserIdentity(user));
            }
        }
    }
}
