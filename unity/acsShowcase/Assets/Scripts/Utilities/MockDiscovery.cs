// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;


namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// A class to help discover if the application is using mocks
    /// </summary>
    internal class MockDiscovery
    {
        private static string _mockDll = "Azure.Communication.Calling.Mock";
        private string _usingDll = null;

        /// <summary>
        /// Is the application using the mock version of ACS.
        /// </summary>
        public bool IsMock
        {
            get
            {
                if (_usingDll == null)
                {

                    Type callClientType = typeof(CallClient);
                    _usingDll = callClientType.Assembly.GetName().Name;
                }

                return _usingDll == _mockDll;
            }

        }

    }
}
