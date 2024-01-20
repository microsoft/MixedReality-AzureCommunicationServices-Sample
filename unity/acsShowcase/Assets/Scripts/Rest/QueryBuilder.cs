// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Text;

namespace Azure.Communication.Calling.Unity.Rest
{
    /// <summary>
    /// A helper class used to create query string.
    /// </summary>
    internal class QueryBuilder

    {
        private int _maxArguments;
        private int _currentArgument;
        private string[] _keys;
        private string[] _values;
        private string _resourceUrl;

        internal QueryBuilder(string resourceUrl, int maxArguments)
        {
            _resourceUrl = resourceUrl;
            _currentArgument = 0;
            _maxArguments = maxArguments;
            _keys = new string[maxArguments];
            _values = new string[maxArguments];
        }

        internal void InsertArgument(string key, string value)
        {
            if (_currentArgument == _maxArguments)
            {
                throw new InvalidOperationException("Query argument can't be added. Max entries already reached.");
            }

            int index = _currentArgument++;
            _keys[index] = key;
            _values[index] = value;
        }

        /// <summary>
        /// Get the URL with query string
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_resourceUrl);
            for (int i = 0; i < _currentArgument; i++)
            {
                if (i == 0)
                {
                    sb.Append('?');
                }
                else
                {
                    sb.Append('&');
                }
                sb.Append(_keys[i]);
                sb.Append('=');
                sb.Append(_values[i]);
            }
            return sb.ToString();
        }
    }
}
