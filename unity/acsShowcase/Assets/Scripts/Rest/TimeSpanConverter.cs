// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Newtonsoft.Json;
using System;

namespace Azure.Communication.Calling.Unity.Rest
{
    /// <summary>
    /// Converts a <see cref="TimeSpan"/> (omits Day part) to and from a ISO 8601 Duration-Time <see href="https://en.wikipedia.org/wiki/ISO_8601#Durations"/>.
    /// </summary>
    public class TimeSpanConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var isValueTimeSpan = value is TimeSpan;

            if (!isValueTimeSpan)
            {
                throw new JsonSerializationException("Expected TimeSpan object value.");
            }

            var timeSpan = (TimeSpan)value;

            var text = string.Format("PT{0}H{1}M{2}S", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            writer.WriteValue(text);
        }

        private bool IsNullableType(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = IsNullableType(objectType);

            if (reader.TokenType == JsonToken.Null)
            {
                if (!isNullable)
                {
                    throw new JsonSerializationException(string.Format("Cannot convert null value to {0}.", objectType));
                }

                return null;
            }

            Type t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;


            if (reader.TokenType == JsonToken.String)
            {
                var timeSpanText = reader.Value.ToString();

                if (!timeSpanText.StartsWith("PT"))
                {
                    throw new JsonSerializationException("TimeSpan text should strat with PT");
                }

                if (!timeSpanText.Contains("H"))
                {
                    throw new JsonSerializationException("TimeSpan text should contain H");
                }
                if (!timeSpanText.Contains("M"))
                {
                    throw new JsonSerializationException("TimeSpan text should contain M");
                }
                if (!timeSpanText.Contains("S"))
                {
                    throw new JsonSerializationException("TimeSpan text should contain S");
                }

                var indexOfH = timeSpanText.IndexOf("H");
                var indexOfM = timeSpanText.IndexOf("M");
                var indexOfS = timeSpanText.IndexOf("S");

                int hours;

                if (!Int32.TryParse(timeSpanText.Substring(2, indexOfH - 2), out hours))
                {
                    throw new JsonSerializationException("H could not be converted to Int32");
                }

                int minutes;
                if (!Int32.TryParse(timeSpanText.Substring(indexOfH + 1, indexOfM - indexOfH - 1), out minutes))
                {
                    throw new JsonSerializationException("M could not be converted to Int32");
                }

                int seconds;
                if (!Int32.TryParse(timeSpanText.Substring(indexOfM + 1, indexOfS - indexOfM - 1), out seconds))
                {
                    throw new JsonSerializationException("S could not be converted to Int32");
                }

                return new TimeSpan(hours, minutes, seconds);
            }


            throw new JsonSerializationException(string.Format("Unexpected token {0} when parsing TimeSpan.", reader.TokenType));
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {

            Type t = IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;

            return typeof(TimeSpan) == t;
        }
    }
}
