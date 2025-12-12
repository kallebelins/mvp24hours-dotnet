//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult"/>
    /// </summary>
    [DataContract, Serializable]
    [method: JsonConstructor]
    public class MessageResult(string? key, string message, MessageType type) : BaseVO, IMessageResult
    {
        #region [ Ctor ]
        public MessageResult(string message, MessageType type)
            : this(null, message, type)
        {
        }

        public MessageResult(string message, string customType)
           : this(message, MessageType.Custom, customType)
        {
        }

        public MessageResult(string message, MessageType type, string? customType)
            : this(null, message, type, customType)
        {
        }

        public MessageResult(string? key, string message, MessageType type, string? customType)
            : this(key, message, type)
        {
            CustomType = customType ?? string.Empty;
        }
        #endregion

        #region [ Properties ]
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult.Key"/>
        /// </summary>
        [DataMember]
        public string? Key { get; } = key;
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult.Message"/>
        /// </summary>
        [DataMember]
        public string Message { get; } = message;
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult.Type"/>
        /// </summary>
        [DataMember]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public MessageType Type { get; } = type;
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult.CustomType"/>
        /// </summary>
        [DataMember]
        public string CustomType { get; } = string.Empty;
        #endregion

        #region [ Methods ]
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Key ?? string.Empty;
            yield return Message;
            yield return Type;
            yield return CustomType;
        }
        #endregion
    }
}
