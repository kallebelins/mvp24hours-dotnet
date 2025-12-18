//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.Logic.Resilience
{
    /// <summary>
    /// Implementation of <see cref="IResultMessage"/> with severity and structured error codes.
    /// </summary>
    [DataContract, Serializable]
    public class ResultMessage : BaseVO, IResultMessage
    {
        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of <see cref="ResultMessage"/>.
        /// </summary>
        [JsonConstructor]
        public ResultMessage(
            MessageSeverity severity,
            string message,
            string? errorCode = null,
            string? propertyName = null,
            object? attemptedValue = null,
            string? key = null,
            IDictionary<string, object?>? metadata = null)
        {
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ErrorCode = errorCode;
            PropertyName = propertyName;
            AttemptedValue = attemptedValue;
            Key = key ?? errorCode;
            Metadata = metadata;
            CustomType = string.Empty;

            // Map severity to MessageType for backward compatibility
            Type = severity switch
            {
                MessageSeverity.Info => MessageType.Info,
                MessageSeverity.Warning => MessageType.Warning,
                MessageSeverity.Error => MessageType.Error,
                _ => MessageType.Custom
            };
        }

        /// <summary>
        /// Creates an error message.
        /// </summary>
        public static ResultMessage Error(
            string message,
            string? errorCode = null,
            string? propertyName = null,
            object? attemptedValue = null)
        {
            return new ResultMessage(
                MessageSeverity.Error,
                message,
                errorCode,
                propertyName,
                attemptedValue);
        }

        /// <summary>
        /// Creates a warning message.
        /// </summary>
        public static ResultMessage Warning(
            string message,
            string? errorCode = null,
            string? propertyName = null)
        {
            return new ResultMessage(
                MessageSeverity.Warning,
                message,
                errorCode,
                propertyName);
        }

        /// <summary>
        /// Creates an info message.
        /// </summary>
        public static ResultMessage Info(string message, string? key = null)
        {
            return new ResultMessage(
                MessageSeverity.Info,
                message,
                key: key);
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc/>
        [DataMember]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageSeverity Severity { get; }

        /// <inheritdoc/>
        [DataMember]
        public string? ErrorCode { get; }

        /// <inheritdoc/>
        [DataMember]
        public string? PropertyName { get; }

        /// <inheritdoc/>
        [DataMember]
        public object? AttemptedValue { get; }

        /// <inheritdoc/>
        [DataMember]
        public IDictionary<string, object?>? Metadata { get; }

        /// <inheritdoc/>
        [DataMember]
        public string? Key { get; }

        /// <inheritdoc/>
        [DataMember]
        public string Message { get; }

        /// <inheritdoc/>
        [DataMember]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageType Type { get; }

        /// <inheritdoc/>
        [DataMember]
        public string CustomType { get; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Returns a string representation of the message.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string> { $"[{Severity}]" };

            if (!string.IsNullOrEmpty(ErrorCode))
                parts.Add($"({ErrorCode})");

            if (!string.IsNullOrEmpty(PropertyName))
                parts.Add($"{PropertyName}:");

            parts.Add(Message);

            return string.Join(" ", parts);
        }

        /// <inheritdoc/>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Severity;
            yield return Message;
            yield return ErrorCode ?? string.Empty;
            yield return PropertyName ?? string.Empty;
            yield return Key ?? string.Empty;
        }

        #endregion
    }
}

