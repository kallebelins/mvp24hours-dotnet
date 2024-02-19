//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Pipe;
using System.Collections.Generic;
using System.ServiceModel.Channels;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class PipelineMessageExtensions
    {
        /// <summary>
        /// Encapsulates object for pipeline message
        /// </summary>
        public static IPipelineMessage ToMessage<T>(this T value)
        {
            return ToMessageWithToken<T>(value, null);
        }

        /// <summary>
        /// Encapsulates object for pipeline message
        /// </summary>
        public static IPipelineMessage ToMessage<T>(this T value, string keyContent)
        {
            return ToMessageWithToken<T>(value, keyContent, null);
        }

        /// <summary>
        /// Transform business object to pipeline message
        /// </summary>
        public static IPipelineMessage ToMessage<T>(IBusinessResult<T> bo)
        {
            return ToMessageWithToken<T>(bo, null);
        }

        /// <summary>
        /// Encapsulates object for pipeline message
        /// </summary>
        public static IPipelineMessage ToMessageWithToken<T>(this T value, string keyContent, string tokenDefault)
        {
            var message = new PipelineMessage(tokenDefault);
            if (value != null)
            {
                message.AddContent(keyContent, value);
            }
            return message;
        }

        /// <summary>
        /// Encapsulates object for pipeline message
        /// </summary>
        public static IPipelineMessage ToMessageWithToken<T>(this T value, string tokenDefault)
        {
            var message = new PipelineMessage(tokenDefault);
            if (value != null)
            {
                message.AddContent(value);
            }
            return message;
        }

        /// <summary>
        /// Transform business object to pipeline message
        /// </summary>
        public static IPipelineMessage ToMessageWithToken<T>(IBusinessResult<T> bo, string tokenDefault)
        {
            var message = new PipelineMessage(bo?.Token ?? tokenDefault);
            if (bo != null && bo.Data != null)
            {
                if (bo.Data.IsList())
                {
                    foreach (var item in bo.Data as IEnumerable<object>)
                    {
                        message.AddContent(item);
                    }
                }
                else
                {
                    message.AddContent(bo.Data);
                }
            }
            return message;
        }

        /// <summary>
        /// Transform business object to pipeline message and clone content
        /// </summary>
        public static IPipelineMessage ToMessageClone<T>(IBusinessResult<T> bo)
        {
            return ToMessageCloneWithToken<T>(bo, null);
        }

        /// <summary>
        /// Transform business object to pipeline message and clone content
        /// </summary>
        public static IPipelineMessage ToMessageCloneWithToken<T>(IBusinessResult<T> bo, string tokenDefault)
        {
            var message = new PipelineMessage(bo?.Token ?? tokenDefault);
            if (bo != null && bo.Data != null)
            {
                if (bo.Data.IsList())
                {
                    foreach (var item in bo.Data as IEnumerable<object>)
                    {
                        message.AddContent(ObjectHelper.Clone(item));
                    }
                }
                else
                {
                    message.AddContent(ObjectHelper.Clone(bo.Data));
                }
            }
            return message;
        }

        /// <summary>
        /// Add message to a message list
        /// </summary>
        public static IList<IMessageResult> AddMessage(this IList<IMessageResult> messages, string key, string message, MessageType messageType)
        {
            return messages.AddMessage(new MessageResult(key, message, messageType));
        }

        /// <summary>
        /// Add message to a message list
        /// </summary>
        public static IList<IMessageResult> AddMessage(this IList<IMessageResult> messages, string message, MessageType messageType)
        {
            return messages.AddMessage(new MessageResult(message, messageType));
        }

        /// <summary>
        /// Add message to a message list
        /// </summary>
        public static IList<IMessageResult> AddMessage(this IList<IMessageResult> messages, IMessageResult message)
        {
            if (messages == null || message == null) return messages;
            messages.Add(message);
            return messages;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorMessage"></param>
        public static void AddError(this IPipelineMessage pipelineMessage, string errorMessage)
        {
            pipelineMessage.Messages.Add(errorMessage.ToMessageResult(MessageType.Error));
        }
    }
}
