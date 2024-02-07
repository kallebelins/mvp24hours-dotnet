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
    [DataContract, Serializable]
    public class BusinessResult<T>(
        T data = default,
        IReadOnlyCollection<IMessageResult> messages = null,
        string token = null
        ) : BaseVO, IBusinessResult<T>
    {
        #region [ Properties ]

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Data"/>
        /// </summary>
        [DataMember]
        public T Data { get; } = data;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Messages"/>
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<IMessageResult> Messages { get; } = messages;

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.HasErrors"/>
        /// </summary>
        [DataMember]
        public bool HasErrors => Messages != null && Messages.Any(x => x.Type == Enums.MessageType.Error);

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessResult{T}.Token"/>
        /// </summary>
        [DataMember]
        public string Token { get; private set; } = token;

        #endregion

        #region [ Methods ]

        public void SetToken(string token)
        {
            if (string.IsNullOrEmpty(this.Token)
                && !string.IsNullOrEmpty(token))
            {
                this.Token = token;
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Token;
            yield return Data;
            yield return Messages;
        }

        #endregion
    }
}
