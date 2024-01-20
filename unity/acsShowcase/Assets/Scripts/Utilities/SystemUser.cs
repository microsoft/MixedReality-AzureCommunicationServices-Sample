// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Threading.Tasks;

#if WINDOWS_UWP
using System.Collections.Generic;
using System.Linq;
using Windows.System;
#endif

#if UNITY_EDITOR_WIN && WINDOWS_UWP
using System.Security.Principal;
#endif

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// A helper to load the system's local user information.
    /// </summary>
    public class SystemUser
    {
        public const string UNKNOWN_USER = "Unknown User";

        /// <summary>
        /// Get the name of the user currently signed into the operating system.
        /// </summary>
        public static async Task<string> GetName()
        {
            string displayName = null;
            try
            {
                displayName = await GetUserNameWorker();
            }
            catch (Exception ex)
            {
                Log.Error<SystemUser>("Failed to load user's name. {0}", ex);
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = UNKNOWN_USER;
            }

            return displayName;
        }

#if UNITY_EDITOR_WIN && WINDOWS_UWP
        private static Task<string> GetUserNameWorker()
        {
            var user = WindowsIdentity.GetCurrent();
            string displayName = user?.Name;
            return Task.FromResult(displayName);
        }
#elif WINDOWS_UWP
        private static async Task<string> GetUserNameWorker()
        {
            IReadOnlyList<User> users = await User.FindAllAsync();
            if (users == null)
            {
                Log.Error<SystemUser>("Unable to get user name. Users was null.");
                return null;
            }

            var current = users.FirstOrDefault();
            if (current == null)
            {
                Log.Error<SystemUser>("Unable to get user name. No current user found.");
                return null;
            }

            // Try find first and last name
            string displayName = null;
            string first = await current.GetPropertyAsync(KnownUserProperties.FirstName) as string;
            string last = await current.GetPropertyAsync(KnownUserProperties.LastName) as string;

            bool emptyFirst = string.IsNullOrEmpty(first);
            bool emptyLast = string.IsNullOrEmpty(last);

            if (!emptyFirst && !emptyLast)
            {
                displayName = string.Format("{0} {1}", first, last);
            }
            else if (emptyFirst && emptyLast)
            {
                displayName = UNKNOWN_USER;
            }
            else if (!emptyFirst)
            {
                displayName = first;
            }
            else if (!emptyLast)
            {
                displayName = last;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                // User may have username
                Log.Error<SystemUser>("Failed to find first and/or last name property.");
                var data = await current.GetPropertyAsync(KnownUserProperties.AccountName);
                displayName = data as string;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                Log.Error<SystemUser>("Failed to find user display name.");
            }
            else
            {
                Log.Verbose<SystemUser>("Display name found. {0}", displayName);
            }

            return displayName;
        }
#else
        private static Task<string> GetUserNameWorker()
        {
            return Task.FromResult(string.Empty);
        }
#endif
    }
}
