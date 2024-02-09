//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class MessageResultExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<IMessageResult> ToMessageResult(this IEnumerable<string> messages, MessageType messageType)
        {
            var messagesResult = new List<IMessageResult>();
            foreach (var item in messages)
            {
                messagesResult.Add(new MessageResult(item ?? "Undefined message.", messageType));
            }

            return messagesResult;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<IMessageResult> ToMessageResult(this IEnumerable<string> messages, MessageType messageType, string customMessageType)
        {
            var messagesResult = new List<IMessageResult>();
            foreach (var item in messages)
            {
                messagesResult.Add(new MessageResult(item ?? "Undefined message.", messageType, customMessageType));
            }

            return messagesResult;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IMessageResult ToMessageResult(this string message, string key, MessageType messageType)
        {
            return new MessageResult(key, message ?? "Undefined message.", messageType);
        }

        /// <summary>
        /// 
        /// </summary>
        public static IMessageResult ToMessageResult(this string message, MessageType messageType)
        {
            return new MessageResult(message ?? "Undefined message.", messageType);
        }

        /// <summary>
        /// 
        /// </summary>
        public static IMessageResult ToMessageResult(this string message, MessageType messageType, string customMessageType)
        {
            return new MessageResult(message ?? "Undefined message.", messageType, customMessageType);
        }

        /// <summary>
        /// 
        /// </summary>
        public static IMessageResult ToMessageResult(this string message, string customMessageType)
        {
            return new MessageResult(message ?? "Undefined message.", MessageType.Custom, customMessageType);
        }


        /// <summary>
        /// 
        /// </summary>
        public static IMessageResult ToMessageResult(this Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return new MessageResult((exception.InnerException ?? exception).Message, MessageType.Error);
        }
    }
}
