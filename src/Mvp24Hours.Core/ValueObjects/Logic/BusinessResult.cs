//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IBusinessResult{T}"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BusinessResult class encapsulates the result of a business operation,
    /// containing data, messages, and a token for transaction tracking.
    /// </para>
    /// <para>
    /// <strong>Implicit Operators:</strong>
    /// <list type="bullet">
    /// <item><c>T → BusinessResult&lt;T&gt;</c>: Wraps data in a success result</item>
    /// <item><c>BusinessResult&lt;T&gt; → T</c>: Extracts data (use with caution)</item>
    /// <item><c>BusinessResult&lt;T&gt; → bool</c>: Returns true if no errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implicit conversion from data
    /// BusinessResult&lt;Order&gt; result = order;
    /// 
    /// // Implicit conversion to bool
    /// if (result) { /* success */ }
    /// 
    /// // Implicit conversion to data
    /// Order data = result;
    /// </code>
    /// </example>
    [DataContract, Serializable]
    public class BusinessResult<T>(
        T? data = default,
        IReadOnlyCollection<IMessageResult>? messages = null,
        string? token = null
        ) : BaseVO, IBusinessResult<T>
    {
        #region [ Properties ]

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Data"/>
        /// </summary>
        [DataMember]
        public T? Data { get; } = data;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Messages"/>
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<IMessageResult>? Messages { get; } = messages;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.HasErrors"/>
        /// </summary>
        [DataMember]
        public bool HasErrors => Messages != null && Messages.Any(x => x.Type == Enums.MessageType.Error);

        /// <summary>
        /// Indicates whether the result is successful (no errors).
        /// </summary>
        [IgnoreDataMember]
        public bool IsSuccess => !HasErrors;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Token"/>
        /// </summary>
        [DataMember]
        public string? Token { get; private set; } = token;

        #endregion

        #region [ Methods ]

        public void SetToken(string? token)
        {
            if (string.IsNullOrEmpty(this.Token)
                && !string.IsNullOrEmpty(token))
            {
                this.Token = token;
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Token ?? string.Empty;
            yield return Data!;
            yield return Messages!;
        }

        #endregion

        #region [ Implicit Operators ]

        /// <summary>
        /// Implicitly converts data to a successful BusinessResult.
        /// </summary>
        /// <param name="data">The data to wrap.</param>
        /// <returns>A BusinessResult containing the data.</returns>
        /// <example>
        /// <code>
        /// BusinessResult&lt;Order&gt; result = order; // Creates success result
        /// </code>
        /// </example>
        public static implicit operator BusinessResult<T>(T data)
        {
            return new BusinessResult<T>(data: data);
        }

        /// <summary>
        /// Implicitly extracts data from a BusinessResult.
        /// </summary>
        /// <param name="result">The result to extract data from.</param>
        /// <returns>The data contained in the result.</returns>
        /// <remarks>
        /// <strong>Warning:</strong> This will return default(T) if result is null or has errors.
        /// Use <see cref="Data"/> property directly for explicit access.
        /// </remarks>
        public static implicit operator T?(BusinessResult<T>? result)
        {
            return result != null ? result.Data : default;
        }

        /// <summary>
        /// Implicitly converts a BusinessResult to a boolean.
        /// </summary>
        /// <param name="result">The result to check.</param>
        /// <returns>True if the result is successful (no errors), false otherwise.</returns>
        /// <example>
        /// <code>
        /// var result = GetOrder();
        /// if (result) { /* process success */ }
        /// </code>
        /// </example>
        public static implicit operator bool(BusinessResult<T> result)
        {
            return result != null && result.IsSuccess;
        }

        #endregion
    }
}
