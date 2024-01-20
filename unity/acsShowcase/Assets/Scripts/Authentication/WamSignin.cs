// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNITY_WSA && !UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Popups;
using UnityEngine;
using System.Threading;
using Microsoft.Identity.Client;

namespace Azure.Communication.Calling.Unity
{
    public class WamSignin : ISignin
    {
        [Serializable]
        public class AccountCache
        {
            public string Authority = null;
            public string ProviderId = null;
            public string AccountId = null;
        };

        private static string CacheFilePath
        {
            get
            {
                var local = ApplicationData.Current.LocalFolder;
                var basePath = local.Path;
                return Path.Combine(basePath, "accountcache.json");
            }
        }

        private const string MICROSOFT_PROVIDER_ID = "https://login.microsoft.com";
        private const string CONSUMER_AUTHORITY = "consumers";
        private const string ORGANIZATION_AUTHORITY = "organizations";
        private const string USER_PROPERTY_NAME_AAD_OBJECT_ID = "OID";
        private static readonly string[] SUPPORTED_AUTHORITIES = {
            CONSUMER_AUTHORITY,
            ORGANIZATION_AUTHORITY
        };

        private WebAccount _account;
        private string _authority;
        private string _clientId;
        private string _tenantId;

        public void Initialize(string clientId, string authority, string tenantId)
        {
            try
            {
                // Print out the reply URL that the Azure application should be configured with for this app.
                // FIXME - Remove direct reference to Debug.Log() which bypasses the MeshProjectSettings.logLevel setting
                Debug.Log($"ReplyURL should be 'ms-appx-web://Microsoft.AAD.BrokerPlugIn/{WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper()}'");
            }
            catch
            {
                // If running as something other than UWP, this may rightfully throw an exception.
            }
            _authority = authority;
            _clientId = clientId;
            _tenantId = tenantId;
        }

        public Task<TokenResponse> GetTokenAsync(string[] scopes)
        {
            return GetTokenAsync(scopes, resource: null, CancellationToken.None);
        }

        public Task<TokenResponse> GetTokenAsync(string[] scopes, string resource)
        {
            return GetTokenAsync(scopes, resource, CancellationToken.None);
        }

        public Task<TokenResponse> GetTokenAsync(string[] scopes, CancellationToken cancellationToken)
        {
            return GetTokenAsync(scopes, resource: null, cancellationToken);
        }

        public async Task<TokenResponse> GetTokenAsync(string[] scopes, string resource, CancellationToken cancellationToken)
        {
            var response = new TokenResponse()
            {
                ClientId = _clientId,
                TenantId = _tenantId
            };


            var op = new LoginOperation(_clientId, _tenantId, scopes, resource, cancellationToken);
            _account = await op.LoginAsync();
            response.Token = op.Token;
            response.UserId = string.Empty;
            if (_account.Properties != null && _account.Properties.ContainsKey(USER_PROPERTY_NAME_AAD_OBJECT_ID))
            {
                response.UserId = _account.Properties[USER_PROPERTY_NAME_AAD_OBJECT_ID];
            }
            else
            {
                Log.Warning<WamSignin>("Failed to locate user's object id. Some functionality may not work.");
            }
            return response;
        }

        public async Task ClearCacheAsync()
        {
            if (_account != null)
            {
                await _account.SignOutAsync();
                _account = null;
            }
            File.Delete(CacheFilePath);
        }

        public string GetDeviceCode()
        {
            throw new NotImplementedException();
        }

        private class LoginOperation
        {
            public LoginOperation(string clientId, string tenantId, string[] scopes, string resource, CancellationToken cancellationToken)
            {
                _clientId = clientId;
                _tenantId = tenantId;
                _scopes = scopes;
                _resource = resource;
                _cancellationToken = cancellationToken;
                var view = CoreApplication.MainView;
                if (view != null)
                {
                    _dispatcher = view.Dispatcher;
                }
            }

            public WebAccount Account { get; private set; }
            public string Token { get; private set; }

            private TaskCompletionSource<WebAccount> _tcs;
            private bool _webAccountProviderCommandInvoked = false;
            private CoreDispatcher _dispatcher;
            private string _clientId;
            private string[] _scopes;
            private string _resource;
            private string _tenantId;
            private CancellationToken _cancellationToken;

            public async Task<WebAccount> LoginAsync()
            {
                _tcs = new TaskCompletionSource<WebAccount>();

                try
                {
                    var account = await LoadAccountAsync();
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        _tcs.TrySetException(new TaskCanceledException());
                    }
                    else if (account == null)
                    {
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            if (_cancellationToken.IsCancellationRequested)
                            {
                                _tcs.TrySetCanceled();
                                return;
                            }

                            var pane = AccountsSettingsPane.GetForCurrentView();
                            pane.AccountCommandsRequested += OnAccountCommandsRequested;
                            await AccountsSettingsPane.ShowManageAccountsAsync();
                            pane.AccountCommandsRequested -= OnAccountCommandsRequested;

                            if (_cancellationToken.IsCancellationRequested)
                            {
                                _tcs.TrySetCanceled();
                            }

                            if (!_webAccountProviderCommandInvoked)
                            {
                                _tcs.TrySetException(new OperationCanceledException("Account selection canceled."));
                            }
                        });
                    }
                    else
                    {
                        Account = account;
                        await AuthenticateWithRequestTokenSilent(Account.WebAccountProvider);
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            _tcs.TrySetCanceled();
                        }
                        else
                        {
                            _tcs.TrySetResult(Account);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }

                return await _tcs.Task;
            }

            private async void OnAccountCommandsRequested(AccountsSettingsPane sender, AccountsSettingsPaneCommandsRequestedEventArgs args)
            {
                args.Commands.Clear();
                args.WebAccountCommands.Clear();
                var deferral = args.GetDeferral();
                if (_cancellationToken.IsCancellationRequested)
                {
                    deferral.Complete();
                    return;
                }

                var account = await LoadAccountAsync();
                if (_cancellationToken.IsCancellationRequested)
                {
                    deferral.Complete();
                    return;
                }

                if (account != null)
                {
                    var accountCommand = new WebAccountCommand(account, OnWebAccountCommandInvoked, SupportedWebAccountActions.Remove);
                    args.WebAccountCommands.Add(accountCommand);
                }
                else
                {
                    await AddWebAccountProviders(args);
                }

                if (_cancellationToken.IsCancellationRequested)
                {
                    args.Commands.Clear();
                    args.WebAccountCommands.Clear();
                    deferral.Complete();
                    return;
                }

                AddLinksAndDescription(args);
                deferral.Complete();
            }

            private void OnWebAccountCommandInvoked(WebAccountCommand command, WebAccountInvokedArgs args)
            {
                if (args.Action == WebAccountAction.Remove)
                {
                    Account = null;
                    File.Delete(CacheFilePath);
                }
            }

            private void AddLinksAndDescription(AccountsSettingsPaneCommandsRequestedEventArgs args)
            {
                args.HeaderText = "Describe what adding an account to your application will do for the user.";

                //args.Commands.Add(new SettingsCommand("privacypolicy", "Privacy Policy", OnPrivacyPolicyInvoked));

                // This is how additional links can be added. We have no used for this at the moment.
                //args.Commands.Add(new SettingsCommand("otherlink", "Other Locator", OnOtherLinkInvoked));
            }

            private async Task AddWebAccountProviders(AccountsSettingsPaneCommandsRequestedEventArgs args)
            {
                args.WebAccountProviderCommands.Clear();

                foreach (var authority in SUPPORTED_AUTHORITIES)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }    

                    var foundProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(MICROSOFT_PROVIDER_ID, authority);
                    var providerCommand = new WebAccountProviderCommand(foundProvider, OnWebAccountProviderCommandInvoked);
                    args.WebAccountProviderCommands.Add(providerCommand);
                }
            }

            private async void OnWebAccountProviderCommandInvoked(WebAccountProviderCommand command)
            {
                _webAccountProviderCommandInvoked = true;
                try
                {
                    await AuthenticateWithRequestToken(command.WebAccountProvider);

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        _tcs.TrySetCanceled();
                    }
                    else
                    {
                        _tcs.TrySetResult(Account);
                    }
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            private async Task AuthenticateWithRequestTokenSilent(WebAccountProvider provider, bool allowInteractive = true)
            {
                var wtr = MakeWebTokenRequestForScopes(provider);
                var wtrResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr, Account);
                await HandleWebTokenRequestResult(provider, wtrResult, allowInteractive);
            }

            private async Task AuthenticateWithRequestToken(WebAccountProvider provider)
            {
                var wtr = MakeWebTokenRequestForScopes(provider);
                var webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
                await HandleWebTokenRequestResult(provider, webTokenRequestResult);
            }

            private WebTokenRequest MakeWebTokenRequestForScopes(WebAccountProvider provider)
            {
                var authority = "https://login.windows.net/common";
                if (!String.IsNullOrEmpty(_tenantId))
                {
                    authority = $"https://login.microsoftonline.com/{_tenantId}";
                }

                string resource = null;
                if (!string.IsNullOrWhiteSpace(_resource))
                {
                    resource = _resource;
                }

                foreach (var scope in _scopes)
                {
                    if (Uri.TryCreate(scope, UriKind.Absolute, out Uri uri))
                    {
                        bool hostIsClient = (String.Compare(uri.Host, _clientId, ignoreCase: true) == 0);

                        string newResource = null;
                        if (hostIsClient)
                        {
                            newResource = uri.Host;
                        }
                        else
                        {
                            newResource = $"{uri.Scheme}://{uri.Host}";

                            var localParts = uri.LocalPath.Split('/');

                            // ignore first local part, this is the empty string before the slash
                            // ignore last local part, this is the scope
                            for (int i = 1; i < (localParts.Length - 1); i++)
                            {
                                // Asking for a token from the client id itself.
                                // In which case the resource needs to be the client id.
                                if (i == 1 && localParts[i] == _clientId)
                                {
                                    newResource = _clientId;
                                    break;
                                }

                                newResource += $"/{localParts[i]}";
                            }

                            Log.Verbose<WamSignin>("Found new resource '{0}'", newResource);
                        }

                        if (resource != null && resource != newResource)
                        {
                            throw new InvalidOperationException($"{scope} is not compatible with previous scopes; GetTokenAsync must called once per incompatible scope. {resource} != {newResource}");
                        }
                        resource = newResource;
                    }
                }

                var wtr = new WebTokenRequest(provider, string.Join(" ", _scopes), _clientId);
                wtr.Properties.Add("authority", authority);

                if (!string.IsNullOrEmpty(resource))
                {
                    wtr.Properties.Add("resource", resource);
                }
                else
                {
                    wtr.Properties.Add("wam_compat", "2.0");
                }

                if (provider.Authority != ORGANIZATION_AUTHORITY)
                {
                    Log.Verbose<WamSignin>("Authority is not an organization authority. {0} <> {1}", ORGANIZATION_AUTHORITY, provider.Authority);
                    wtr.Properties.Add("api-version", "2.0");
                    wtr.Properties.Add("client_id", _clientId);
                }

                return wtr;
            }

            private async Task HandleWebTokenRequestResult(WebAccountProvider provider, WebTokenRequestResult webTokenRequestResult, bool allowInteractive = true)
            {
                var responseStatus = webTokenRequestResult.ResponseStatus;
                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                else if (responseStatus == WebTokenRequestStatus.Success)
                {
                    var response = webTokenRequestResult.ResponseData[0];
                    if (response.WebAccount != null)
                    {                        
                        Account = response.WebAccount;
                        Token = response.Token;
                        SaveAccount(Account);
                    }
                }
                else if (responseStatus == WebTokenRequestStatus.UserCancel && allowInteractive)
                {
                    var op = new LoginOperation(_clientId, _tenantId, _scopes, null, _cancellationToken);
                    Account = await op.LoginAsync();
                    Token = op.Token;
                }
                else if (responseStatus == WebTokenRequestStatus.UserInteractionRequired && allowInteractive)
                {
                    await AuthenticateWithRequestToken(provider);
                }
                else
                {
                    var error = webTokenRequestResult.ResponseError;
                    var tokenRequestErrorCode = error.ErrorCode;

                    Account = null;
                    File.Delete(CacheFilePath);

                    throw new InvalidOperationException(error.ErrorMessage);
                }

                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }


            private void OnOtherLinkInvoked(IUICommand command)
            {
                throw new NotImplementedException();
            }

            private void OnPrivacyPolicyInvoked(IUICommand command)
            {
                throw new NotImplementedException();
            }

            private async Task<WebAccount> LoadAccountAsync()
            {
                AccountCache accountCache = null;
                if (!File.Exists(CacheFilePath))
                {
                    Log.Warning<WamSignin>("accountcache file does not exist");
                    return null;
                }

                try
                {
                    var data = File.ReadAllText(CacheFilePath);
                    accountCache = JsonUtility.FromJson<AccountCache>(data);
                }
                catch
                {
                    return null;
                }

                if (string.IsNullOrEmpty(accountCache.AccountId) || string.IsNullOrEmpty(accountCache.ProviderId) || string.IsNullOrEmpty(accountCache.Authority) || accountCache.ProviderId != MICROSOFT_PROVIDER_ID)
                {
                    return null;
                }

                bool found = false;
                foreach (var authority in SUPPORTED_AUTHORITIES)
                {
                    found = found || authority == accountCache.Authority;
                }

                if (!found)
                {
                    return null;
                }

                var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(accountCache.ProviderId, accountCache.Authority);
                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                var result = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountCache.AccountId);
                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                return result;
            }

            private void SaveAccount(WebAccount account)
            {
                AccountCache accountCache = new AccountCache()
                {
                    Authority = account.WebAccountProvider.Authority,
                    ProviderId = account.WebAccountProvider.Id,
                    AccountId = account.Id,
                };

                var data = JsonUtility.ToJson(accountCache);
                File.WriteAllText(CacheFilePath, data);
            }
        }

    }
}
#endif
