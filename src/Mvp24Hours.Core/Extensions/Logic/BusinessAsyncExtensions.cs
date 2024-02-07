//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Core.ValueObjects.Logic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class BusinessAsyncExtensions
    {
        /// <summary>
        /// Transform a message into a business object
        /// </summary>
        public static async Task<IBusinessResult<T>> ToBusinessAsync<T>(this Task<IPipelineMessage> messageAsync, string key = null, string tokenDefault = null)
        {
            var message = await messageAsync;
            if (message != null)
            {
                return new BusinessResult<T>(
                    token: message.Token ?? tokenDefault,
                    data: key.HasValue() ? message.GetContent<T>(key) : message.GetContent<T>(),
                    messages: new ReadOnlyCollection<IMessageResult>(message.Messages ?? new List<IMessageResult>())
                );
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<T>> ToBusinessAsync<T>(this Task<IMessageResult> messageResultAsync, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            var messageResult = await messageResultAsync;
            if (messageResult != null || defaultMessage != null)
            {
                return await ToBusinessAsync<T>(default, new List<IMessageResult> { messageResult }, defaultMessage, tokenDefault);
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<T>> ToBusinessAsync<T>(this Task<IList<IMessageResult>> messageResultAsync, IMessageResult defaultMessage, string tokenDefault = null)
        {
            var messageResult = await messageResultAsync;
            if (messageResult != null || defaultMessage != null)
            {
                return await ToBusinessAsync<T>(default, messageResult, defaultMessage, tokenDefault);
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<T>> ToBusinessAsync<T>(this Task<T> valueAsync, IMessageResult messageResult, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            var value = await valueAsync;
            if (value != null)
            {
                var messages = new List<IMessageResult>();
                if (messageResult != null)
                {
                    messages.Add(messageResult);
                }
                else if (defaultMessage != null)
                {
                    messages.Add(defaultMessage);
                }

                return new BusinessResult<T>(
                    token: tokenDefault,
                    data: value,
                    messages: new ReadOnlyCollection<IMessageResult>(messages)
                );
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<T>> ToBusinessAsync<T>(this Task<T> valueAsync, IList<IMessageResult> messageResult = null, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            var value = await valueAsync;
            if (value != null)
            {
                var messages = new List<IMessageResult>();
                if (messageResult.AnySafe())
                {
                    messages.AddRange(messageResult);
                }
                else if (defaultMessage != null)
                {
                    messages.Add(defaultMessage);
                }

                return new BusinessResult<T>(
                    token: tokenDefault,
                    data: value,
                    messages: new ReadOnlyCollection<IMessageResult>(messages)
                );
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<VoidResult>> ToBusinessAsync(this Task task, IMessageResult messageResult, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            await task;

            var messages = new List<IMessageResult>();
            if (messageResult != null)
            {
                messages.Add(messageResult);
            }
            else if (defaultMessage != null)
            {
                messages.Add(defaultMessage);
            }

            return new BusinessResult<VoidResult>(
                token: tokenDefault,
                messages: new ReadOnlyCollection<IMessageResult>(messages)
            );
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static async Task<IBusinessResult<VoidResult>> ToBusinessAsync(this Task task, IList<IMessageResult> messageResult = null, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            await task;

            var messages = new List<IMessageResult>();
            if (messageResult.AnySafe())
            {
                messages.AddRange(messageResult);
            }
            else if (defaultMessage != null)
            {
                messages.Add(defaultMessage);
            }

            return new BusinessResult<VoidResult>(
                token: tokenDefault,
                messages: new ReadOnlyCollection<IMessageResult>(messages)
            );
        }

        public static async Task<bool> HasDataAsync<T>(this Task<IBusinessResult<T>> valueAsync)
        {
            var value = await valueAsync;
            if (value == null || value.Data == null)
            {
                return false;
            }

            if (value.Data.IsList())
            {
                return (value.Data as IEnumerable<object>).AnySafe();
            }

            return true;
        }

        public static async Task<T> GetDataValueAsync<T>(this Task<IBusinessResult<T>> valueAsync)
        {
            var value = await valueAsync;
            if (value.HasData())
            {
                return value.Data;
            }
            return default;
        }

        public static async Task<int> GetDataCountAsync<T>(this Task<IBusinessResult<T>> valueAsync)
        {
            var value = await valueAsync;
            if (value.HasData())
            {
                if (value.Data.IsList())
                {
                    return (value.Data as IEnumerable<object>).Count();
                }
                else
                {
                    return 1;
                }
            }
            return 0;
        }

        public static async Task<bool> HasDataCountAsync<T>(this Task<IBusinessResult<T>> valueAsync, int count)
        {
            var value = await valueAsync;
            if (value.HasData() && value.Data.IsList())
            {
                return (value.Data as IEnumerable<object>).Count() == count;
            }
            return false;
        }

        public static async Task<object> GetDataFirstOrDefaultAsync<T>(this Task<IBusinessResult<T>> valueAsync)
        {
            var value = await valueAsync;
            if (value.HasData())
            {
                if (value.Data.IsList())
                {
                    return (value.Data as IEnumerable<object>).FirstOrDefault();
                }
                else
                {
                    return value.Data;
                }
            }
            return null;
        }

    }
}
