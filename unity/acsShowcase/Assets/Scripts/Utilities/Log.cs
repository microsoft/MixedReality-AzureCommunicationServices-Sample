// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

namespace Azure.Communication.Calling.Unity
{
    public static class Log
    {
        private static LogOption _option = LogOption.NoStacktrace;
        private static int _level = (int)AppLogLevel.Verbose;
        private static ConcurrentDictionary<Type, string> _names = new ConcurrentDictionary<Type, string>();

        public static bool Enabled(AppLogLevel level)
        {
            return (int)level <= _level;
        }

        private static string Name<T>()
        {
            string result;
            Type type = typeof(T);
            if (!_names.TryGetValue(type, out result))
            {
                result = type.Name;
                _names.TryAdd(type, type.Name);
            }
            return result;
        }

        public static void Verbose<T>(string message)
        {
            if (Enabled(AppLogLevel.Verbose))
            {
                Debug.LogFormat(LogType.Log, _option, context: null, "{0}", Format<T>(message));
            }
        }

        public static void Verbose<T>(string message, params object[] args)
        {
            if (Enabled(AppLogLevel.Verbose))
            {
                Debug.LogFormat(LogType.Log, _option, context: null, "{0}", Format<T>(message, args));
            }
        }

        public static void Information<T>(string message)
        {
            if (Enabled(AppLogLevel.Information))
            {
                Debug.LogFormat(LogType.Log, _option, context: null, "{0}", Format<T>(message));
            }
        }

        public static void Information<T>(string message, params object[] args)
        {
            if (Enabled(AppLogLevel.Information))
            {
                Debug.LogFormat(LogType.Log, _option, context: null, "{0}", Format<T>(message, args));
            }
        }

        public static void Warning<T>(string message)
        {
            if (Enabled(AppLogLevel.Warning))
            {
                Debug.LogFormat(LogType.Warning, _option, context: null, "{0}", Format<T>(message));
            }
        }

        public static void Warning<T>(string message, params object[] args)
        {
            if (Enabled(AppLogLevel.Warning))
            {
                Debug.LogFormat(LogType.Warning, _option, context: null, "{0}", Format<T>(message, args));
            }
        }

        public static void Error<T>(string message)
        {
            if (Enabled(AppLogLevel.Error))
            {
                Debug.LogFormat(LogType.Error, _option, context: null, "{0}", Format<T>(message));
            }
        }

        public static void Error<T>(string message, params object[] args)
        {
            if (Enabled(AppLogLevel.Error))
            {
                Debug.LogFormat(LogType.Error, _option, context: null, "{0}", Format<T>(message, args));
            }
        }

        /// <summary>
        /// Create a time stamp
        /// </summary>
        /// <returns></returns>
        private static string TimeStamp()
        {
            return DateTime.UtcNow.ToString();
        }

        private static string Format<T>(string message)
        {
            return $"[{TimeStamp()}] [{Name<T>()}] {message}";
        }

        private static string Format<T>(string message, params object[] args)
        {
            try
            {
                // expand arrays and hashtables to strings
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = ToString(args[i]);
                }
                return $"[{TimeStamp()}] [{Name<T>()}] {string.Format(message, args)}";
            }

            catch (Exception)
            {
                Debug.LogFormat(LogType.Error, _option, context: null, "AppLog format failure. Fix format string '{0}'.", message);
                return $"[{TimeStamp()}] [{Name<T>()}] {message}";
            }
        }

        /// <summary>
        /// Convert a value to string. If the value is a hashtable or an array, each entry will be converted to a string.
        /// </summary>
        private static string ToString(object value)
        {
            if (value is IDictionary)
            {
                return ToString((IDictionary)value, maxEntries: 10);
            }
            else if (value is ICollection)
            {
                return ToString((ICollection)value, maxEntries: 10);
            }
            else
            {
                return value?.ToString() ?? "NULL";
            }
        }

        /// <summary>
        /// Expand a hashtable to single string. Adding only a max number of entries the string.
        /// </summary>
        private static string ToString(IDictionary table, int maxEntries)
        {
            int count = Math.Min(maxEntries, table.Count);
            int currnet = 0;
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            foreach (var key in table.Keys)
            {
                if (currnet >= count)
                {
                    break;
                }

                if (currnet > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(key.ToString());
                sb.Append(" = ");
                sb.Append(ToString(table[key]));

                currnet++;
            }

            if (count < table.Count)
            {
                sb.Append("...");
            }
            sb.Append("]");

            return sb.ToString();
        }

        /// <summary>
        /// Expand a collection to single string. Adding only a max number of entries the string. 
        /// </summary>
        private static string ToString(ICollection collection, int maxEntries)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            int entry = 0;
            foreach (object value in collection)
            {
                if (entry > maxEntries)
                {
                    sb.Append("...");
                    break;
                }

                if (entry > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(value != null ? value.ToString() : "NULL");
                entry++;
            }
            sb.Append("]");

            return sb.ToString();
        }
    }

    public enum AppLogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Information = 3,
        Verbose = 4,
    }
}