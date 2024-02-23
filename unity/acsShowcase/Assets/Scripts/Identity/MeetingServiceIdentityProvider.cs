// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System.Threading.Tasks;
using System;

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// An interal class used to create identities for use with the Azure Communication Services.
    /// </summary>
    public class MeetingServiceIdentityProvider
    {
        private readonly string[] _servicePermissionScopes = new string[] { "chat", "voip" };

        public async Task<MeetingServiceIdentity> CreateTeamsUser(MeetingServiceIdentityProviderOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.communicationEndpoint))
            {
                Log.Error<MeetingServiceIdentityProvider>("Endpoint string is empty");
                throw new ArgumentException("Endpoint string cannot be empty.");
            }

            string graphToken = options.graphAccessToken.Token;
            string communicationToken = options.communicationUserAccessToken.token;

            if (string.IsNullOrWhiteSpace(communicationToken))
            {
                if (!options.communicationAzureActiveDirectoryAccess.IsValid())
                {
                    Log.Error<MeetingServiceIdentityProvider>("The Azure Active Directory access token with communication scopes is empty");
                    throw new ArgumentException("Azure Active Directory access token with communication scopes is empty");
                }

                if (string.IsNullOrWhiteSpace(options.communicationKey) && !options.functionAppAccess.IsValid())
                {
                    Log.Error<MeetingServiceIdentityProvider>("The Communication Services access key and the Function App access token are empty");
                    throw new ArgumentException("The Communication Services access key and the Function App access token are empty");
                }

                if (!string.IsNullOrWhiteSpace(options.communicationKey))
                {
                    communicationToken = await GetAccessTokenViaKey(options.communicationEndpoint, options.communicationKey, options.communicationAzureActiveDirectoryAccess);
                }
                else if (options.functionAppAccess.IsValid())
                {
                    communicationToken = await GetAccessTokenViaToken(options.functionAppEndpoint, options.functionAppAccess, options.communicationAzureActiveDirectoryAccess);
                }
            }

            if (string.IsNullOrEmpty(communicationToken))
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to obtained communcation token");
            }
            else
            {
                Log.Verbose<MeetingServiceIdentityProvider>("Obtained communcation token");
            }

            TaskCompletionSource<MeetingServiceIdentity> taskMeetingId = new TaskCompletionSource<MeetingServiceIdentity>();
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                try
                {
                    taskMeetingId.TrySetResult(new MeetingServiceIdentity(
                        endpoint: options.communicationEndpoint,
                        communcationAccessToken: communicationToken,
                        graphAccessToken: graphToken,
                        displayName: options.guestName,
                        isGuest: false));
                }
                catch (Exception ex)
                {
                    Log.Verbose<MeetingServiceIdentityProvider>($"Failed to create meeting id. {ex}");
                    taskMeetingId.TrySetException(ex);
                }
            }, waitUntilDone: false);
            return await taskMeetingId.Task;
        }

        public async Task<MeetingServiceIdentity> CreateGuest(MeetingServiceIdentityProviderOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.communicationEndpoint))
            {
                Log.Error<MeetingServiceIdentityProvider>("Endpoint string is empty");
                throw new ArgumentException("Endpoint string cannot be empty.");
            }

            string graphToken = options.graphAccessToken.Token;
            string communicationToken = options.communicationUserAccessToken.token;
            if (string.IsNullOrWhiteSpace(communicationToken))
            {
                if (string.IsNullOrWhiteSpace(options.communicationKey) && !options.functionAppAccess.IsValid())
                {
                    Log.Error<MeetingServiceIdentityProvider>("The Communication Services access key and the Function App access token are empty");
                    throw new ArgumentException("The Communication Services access key and the Function App access token are empty");
                }

                if (string.IsNullOrWhiteSpace(options.guestName))
                {
                    Log.Error<MeetingServiceIdentityProvider>("Display name string is empty");
                    throw new ArgumentException("Display name token  string cannot be empty.");
                }

                if (!string.IsNullOrWhiteSpace(options.communicationKey))
                {
                    communicationToken = await GetAccessTokenForGuestViaKey(options.communicationEndpoint, options.communicationKey);
                }
                else
                {
                    communicationToken = await GetAccessTokenForGuestViaToken(options.functionAppEndpoint, options.functionAppAccess);
                }
            }

            if (string.IsNullOrEmpty(communicationToken))
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to obtained communcation token");
            }
            else
            {
                Log.Verbose<MeetingServiceIdentityProvider>("Obtained communcation token");
            }

            TaskCompletionSource<MeetingServiceIdentity> taskMeetingId = new TaskCompletionSource<MeetingServiceIdentity>();
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                try
                {
                    taskMeetingId.TrySetResult(new MeetingServiceIdentity(
                        endpoint: options.communicationEndpoint,
                        communcationAccessToken: communicationToken,
                        graphAccessToken: graphToken,
                        displayName: options.guestName,
                        isGuest: true));
                }
                catch (Exception ex)
                {
                    Log.Verbose<MeetingServiceIdentityProvider>($"Failed to create meeting id. {ex}");
                    taskMeetingId.TrySetException(ex);
                }
            }, waitUntilDone: false);
            return await taskMeetingId.Task;
        }

        public static string ToConnectionString(string endpoint, string accountKey)
        {
            return $"endpoint={endpoint};accesskey={accountKey}";
        }

        private async Task<string> GetAccessTokenViaToken(string functionAppEndpoint, TokenResponse functionAppAccessToken, TokenResponse communicationsAuthenticationToken)
        {
            Identity.ExchangeAccessTokenResponse exchangeResponse = null;
            try
            {
                exchangeResponse = await Identity.Exchange(functionAppEndpoint, functionAppAccessToken, communicationsAuthenticationToken);
            }
            catch (Exception ex)
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to exchange access token via REST. {0}", ex);
                throw ex;
            }

            return exchangeResponse?.token;
        }

        private async Task<string> GetAccessTokenViaKey(string communicationEndpoint, string communicationKey, TokenResponse communicationsAuthenticationToken)
        {
            Identity.ExchangeAccessTokenResponse exchangeResponse;
            try
            {
                exchangeResponse = await Identity.Exchange(communicationEndpoint, communicationKey, communicationsAuthenticationToken);
            }
            catch (Exception ex)
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to exchange access token via REST. {0}", ex);
                throw ex;
            }

            return exchangeResponse?.token;
        }

        private async Task<string> GetAccessTokenForGuestViaToken(string functionAppEndpoint, TokenResponse functionAppAccess)
        {
            Identity.CreateResponse createResponse;
            try
            {
                createResponse = await Identity.Create(functionAppEndpoint, functionAppAccess, _servicePermissionScopes);
            }
            catch (Exception ex)
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to create identity via REST. {0}", ex);
                throw ex;
            }

            LogCreateTokenResponse(createResponse);
            return createResponse?.accessToken?.token;
        }

        private async Task<string> GetAccessTokenForGuestViaKey(string communicationEndpoint, string communicationKey)
        {
            Identity.CreateResponse createResponse;
            try
            {
                createResponse = await Identity.Create(communicationEndpoint, communicationKey, _servicePermissionScopes);
            }
            catch (Exception ex)
            {
                Log.Error<MeetingServiceIdentityProvider>("Failed to create identity via REST. {0}", ex);
                throw ex;
            }

            LogCreateTokenResponse(createResponse);
            return createResponse?.accessToken?.token;
        }

        private void LogCreateTokenResponse(Identity.CreateResponse createResponse)
        {
            if (createResponse?.identity.id == null)
            {
                Log.Error<MeetingServiceIdentityProvider>("Invalid identity from REST");
            }
            else if (string.IsNullOrEmpty(createResponse?.accessToken?.token))
            {
                Log.Error<MeetingServiceIdentityProvider>("Invalid token from REST");
            }
            else
            {
                Log.Verbose<MeetingServiceIdentityProvider>("Created token via REST");
            }
        }
    }
}