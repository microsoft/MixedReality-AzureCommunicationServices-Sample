// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNITY_EDITOR || !UNITY_ANDROID

#if UNITY_EDITOR || !UNITY_WSA
// Browser auth launches an external, persistent browser window. For some reason MSAL doesn't pop a working window by default for unity. This works around that fact.
#define USE_BROWSER_AUTHENTICATION
#endif

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using UnityEngine;

#if UNITY_EDITOR || (!UNITY_WSA && !UNITY_STANDALONE_OSX)
#if !UNITY_EDITOR_OSX
using System.Security.Cryptography;
#endif
#endif

namespace Azure.Communication.Calling.Unity
{
    public class MsalSignin : ISignin
    {
        private struct CachedAccount : IAccount
        {
            public CachedAccount(SerializeableAccount other)
            {
                this.Username = other.Username;
                this.Environment = other.Environment;
                this.HomeAccountId = new AccountId(other.Identifier, other.ObjectId, other.TenantId);
            }

            public string Username { get; set; }

            public string Environment { get; set; }

            public AccountId HomeAccountId { get; set; }
        }

        [Serializable]
        private struct SerializeableAccount
        {
            public SerializeableAccount(IAccount other)
            {
                this.Username = other.Username;
                this.Environment = other.Environment;
                this.Identifier = other.HomeAccountId.Identifier;
                this.ObjectId = other.HomeAccountId.ObjectId;
                this.TenantId = other.HomeAccountId.TenantId;
            }

            public string Username { get; set; }
            public string Environment { get; set; }
            public string Identifier { get; set; }
            public string ObjectId { get; set; }
            public string TenantId { get; set; }
        }

        private static readonly string AccountKey = "SavedAccount";
        private static readonly string CacheFilePath = Application.persistentDataPath + "/.msalcache.bin3";
        private static readonly object FileLock = new object();

        private IPublicClientApplication PublicClientApp;
        private IAccount Account;
        private SynchronizationContext SynchronizationContext;
        private string ClientId;

        public static string GetDefaultAuthority()
        {
            return AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount.ToString();
        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        public MsalSignin()
        {
            // Get the current synchronization context on which to open the browser window
            SynchronizationContext = SynchronizationContext.Current;

            // Ensure the synchronization context belongs to Unity's main thread
            if (SynchronizationContext?.ToString() != "UnityEngine.UnitySynchronizationContext")
            {
                throw new Exception($"{nameof(MsalSignin)} must be instantiated on Unity's main thread");
            }
        }
#endif

        public void Initialize(string clientId, string authority, string tenantId)
        {
            ClientId = clientId;
            var builder = PublicClientApplicationBuilder.Create(clientId)
                 .WithDefaultRedirectUri()
#if USE_BROWSER_AUTHENTICATION
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                // Set redirect URI to localhost URI and port that we can actually listen on
                .WithRedirectUri(HttpUtility.FindFreeLocalhostRedirectUri())
#else
                .WithRedirectUri("http://localhost")
#endif
#else
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
#endif
                ;

            if (!string.IsNullOrEmpty(authority))
            {
                AadAuthorityAudience audience;
                if (Enum.TryParse<AadAuthorityAudience>(authority, out audience))
                {
                    builder = builder.WithAuthority(audience);
                }
                else
                {
                    builder = builder.WithAuthority(authority);
                }
            }
            if (!String.IsNullOrWhiteSpace(tenantId))
            {
                builder = builder.WithTenantId(tenantId);
            }
            PublicClientApp = builder.Build();

#if UNITY_EDITOR || (!UNITY_WSA && !UNITY_STANDALONE_OSX)
#if !UNITY_EDITOR_OSX
            EnableSerialization(PublicClientApp.UserTokenCache);
#endif
#endif
        }

        public async Task ClearCacheAsync()
        {
#if UNITY_EDITOR || (!UNITY_WSA && !UNITY_STANDALONE_OSX)
#if !UNITY_EDITOR_OSX
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
            }
#endif
#endif
            PlayerPrefs.DeleteKey(AccountKey);

            if (Account != null)
            {
                await PublicClientApp.RemoveAsync(this.Account);
            }

            return;
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
            var result = new TokenResponse()
            {
                ClientId = ClientId
            };

            Account = await LoadAccountAsync();

            AuthenticationResult authResult = null;

            try
            {
                authResult = await PublicClientApp.AcquireTokenSilent(scopes, Account)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException)
            {
#if USE_BROWSER_AUTHENTICATION
                SystemWebViewOptions options = new SystemWebViewOptions()
                {
                    HtmlMessageError = "<p> An error occured: {0}. Details {1}</p><br/><p>Close the application and try again.</p>",
                    HtmlMessageSuccess = "<p> Authentication Success</p><br/><p>Please return to the application.</p>",
                };

                authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
                    .WithUseEmbeddedWebView(false)
                    .WithSystemWebViewOptions(options)
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    .WithCustomWebUi(new DefaultOSBrowserWebUI(options, SynchronizationContext))
#endif
                    .ExecuteAsync(cancellationToken);
#else
                authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
                    .WithAccount(Account)
                    .ExecuteAsync(cancellationToken);
#endif

                Account = authResult.Account;
                SaveAccount();
            }

            if (authResult == null)
            {
                throw new InvalidOperationException("Failed to get authentication token.");
            }


            result.Token = authResult.AccessToken;
            result.UserId = Account.HomeAccountId.ObjectId;
            result.TenantId = Account.HomeAccountId.TenantId;
            return result;
        }

        private async Task<IAccount> LoadAccountAsync()
        {
            try
            {
                string accountString = PlayerPrefs.GetString(AccountKey, "");
                if (!string.IsNullOrWhiteSpace(accountString))
                {
                    var account = JsonUtility.FromJson<SerializeableAccount>(accountString);
                    if (!string.IsNullOrWhiteSpace(account.Username))
                    {
                        return new CachedAccount(account);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning<MsalSignin>("Failed loading local account. Exception: {0}", e);
            }

            var accounts = await PublicClientApp.GetAccountsAsync();
            return accounts.FirstOrDefault();
        }

        private void SaveAccount()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Account.Username))
                {
                    SerializeableAccount serializeableAccount = new SerializeableAccount(Account);
                    string stringAccount = JsonUtility.ToJson(serializeableAccount);
                    PlayerPrefs.SetString(AccountKey, stringAccount);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception e)
            {
                Log.Error<MsalSignin>("Failed saving local account. Exception: {0}", e);
            }
        }

#if UNITY_EDITOR || (!UNITY_WSA && !UNITY_STANDALONE_OSX)
#if !UNITY_EDITOR_OSX
        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                    ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(
                        CacheFilePath,
                        ProtectedData.Protect(
                            args.TokenCache.SerializeMsalV3(),
                            null,
                            DataProtectionScope.CurrentUser));
                }
            }
        }

        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

#endif
#endif
        public string GetDeviceCode()
        {
            throw new NotImplementedException();
        }
    }
}
#endif
