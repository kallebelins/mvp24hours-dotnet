//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class BusinessExtensions
    {
        /// <summary>
        /// Transform a message into a business object
        /// </summary>
        public static IBusinessResult<T> ToBusiness<T>(this IPipelineMessage message, string key = null, string tokenDefault = null)
        {
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
        public static IBusinessResult<T> ToBusiness<T>(this IMessageResult messageResult, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            if (messageResult != null || defaultMessage != null)
            {
                return ToBusiness<T>(default, new List<IMessageResult> { messageResult }, defaultMessage, tokenDefault);
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static IBusinessResult<T> ToBusiness<T>(this IList<IMessageResult> messageResult, IMessageResult defaultMessage = null, string tokenDefault = null)
        {
            if (messageResult != null || defaultMessage != null)
            {
                return ToBusiness<T>(default, messageResult, defaultMessage, tokenDefault);
            }
            return new BusinessResult<T>(token: tokenDefault);
        }

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static IBusinessResult<T> ToBusiness<T>(this T value, IMessageResult messageResult, IMessageResult defaultMessage = null, string tokenDefault = null)
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

        /// <summary>
        /// Encapsulates object for business
        /// </summary>
        public static IBusinessResult<T> ToBusiness<T>(this T value, IList<IMessageResult> messageResult = null, IMessageResult defaultMessage = null, string tokenDefault = null)
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

        public static bool HasData<T>(this IBusinessResult<T> value)
        {
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

        public static bool HasMessageKey<T>(this IBusinessResult<T> value, string key)
        {
            if (value == null || value.Messages == null)
            {
                return false;
            }

            return value.Messages.Any(x => x.Key.HasValue() && x.Key.Equals(key, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public static T GetDataValue<T>(this IBusinessResult<T> value)
        {
            if (value.HasData())
            {
                return value.Data;
            }
            return default;
        }

        public static int GetDataCount<T>(this IBusinessResult<T> value)
        {
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

        public static bool HasDataCount<T>(this IBusinessResult<T> value, int count)
        {
            if (value.HasData() && value.Data.IsList())
            {
                return (value.Data as IEnumerable<object>).Count() == count;
            }
            return false;
        }

        public static T GetDataFirstOrDefault<T>(this IBusinessResult<IList<T>> value)
        {
            if (value.HasData())
            {
                return value.Data.FirstOrDefault();
            }
            return default;
        }
    }
}
