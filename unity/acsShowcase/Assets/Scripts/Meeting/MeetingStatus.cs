// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    public struct MeetingStatus
    {
        private static MockDiscovery _mockDiscovery = new MockDiscovery();

        public static MeetingStatus NoCall(MeetingAuthenticationState authenticationStatus)
        {
            return new MeetingStatus()
            {
                AuthenticationState = authenticationStatus,
                AuthenticationError = MeetingAuthenticationError.None,
                CallState = MeetingCallState.None
            };
        }

        public static MeetingStatus LoggedIn(MeetingCallState callStatus)
        {
            return new MeetingStatus()
            {
                AuthenticationState = MeetingAuthenticationState.LoggedIn,
                AuthenticationError = MeetingAuthenticationError.None,
                CallState = callStatus
            };
        }

        public static MeetingStatus LoginError(MeetingAuthenticationError error = MeetingAuthenticationError.Error)
        {
            return new MeetingStatus()
            {
                AuthenticationState = MeetingAuthenticationState.LoggedOut,
                AuthenticationError = error,
                CallState = MeetingCallState.None
            };
        }

        public MeetingAuthenticationState AuthenticationState { get; private set; }

        public MeetingAuthenticationError AuthenticationError { get; private set; }

        public MeetingCallState CallState { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is MeetingStatus)
            {
                return Equals((MeetingStatus)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{AuthenticationState}:{CallState}";
        }

        public bool Equals(MeetingStatus other)
        {
            return AuthenticationState == other.AuthenticationState &&
                CallState == other.CallState;
        }

        public static bool operator ==(MeetingStatus a, MeetingStatus b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MeetingStatus a, MeetingStatus b)
        {
            return !a.Equals(b);
        }

        public string CreateDisplayString()
        {
            if (AuthenticationState == MeetingAuthenticationState.LoggedIn)
            {
                return CreateDisplayStringWhenLoggedIn();
            }
            else if (AuthenticationError != MeetingAuthenticationError.None)
            {
                return CreateDisplayStringWhenAuthenticationError();
            }
            else
            {
                return CreateDisplayStringWhenNotLoggedIn();
            }
        }

        private string CreateDisplayStringWhenNotLoggedIn()
        {
            switch (AuthenticationState)
            {
                case MeetingAuthenticationState.LoggingIn:
                    return "Signing In...";

                case MeetingAuthenticationState.LoggedIn:
                    return "Signed In";

                case MeetingAuthenticationState.LoggingOut:
                    return "Signing Out...";

                case MeetingAuthenticationState.None:
                case MeetingAuthenticationState.LoggedOut:
                default:
                    return "Logged Out";
            }
        }

        private string CreateDisplayStringWhenAuthenticationError()
        {
            switch (AuthenticationError)
            {
                case MeetingAuthenticationError.None:
                    return "No Error";

                case MeetingAuthenticationError.Error:
                default:
                    return "Sign-in Failed";
            }
        }

        private string CreateDisplayStringWhenLoggedIn()
        {
            string result = string.Empty;

            switch (CallState)
            {
                case MeetingCallState.EarlyMedia:
                case MeetingCallState.Connecting:
                    result = "Connecting...";
                    break;

                case MeetingCallState.Ringing:
                    result = "Ringing...";
                    break;

                case MeetingCallState.Connected:
                    result = "Connected";
                    break;

                case MeetingCallState.LocalHold:
                    result = "Holding...";
                    break;

                case MeetingCallState.Disconnecting:
                    result = "Disconnecting...";
                    break;

                case MeetingCallState.InLobby:
                    result = "In Lobby";
                    break;

                case MeetingCallState.RemoteHold:
                    result = "On Hold";
                    break;

                case MeetingCallState.None:
                case MeetingCallState.Disconnected:
                default:
                    result = "Not in meeting";
                    break;
            }

            if (_mockDiscovery.IsMock)
            {
                result = "Mock: " + result;
            }

            return result;
        }
    }
}