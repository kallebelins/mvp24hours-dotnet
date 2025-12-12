//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessEvent"/>
    /// </summary>
    [DataContract, Serializable]
    public class BusinessEvent : BaseVO, IBusinessEvent
    {
        #region [ Ctor ]

        public BusinessEvent(object data, string? token = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Data is required.");
            }

            Created = DateTime.UtcNow;
            Token = token ?? Guid.NewGuid().ToString();
            DataType = data.GetType();

            if (DataType.IsClass && DataType != typeof(string))
            {
                Data = data.ToSerialize();
            }
            else
            {
                Data = Convert.ToString(data) ?? string.Empty;
            }
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessEvent.Created"/>
        /// </summary>
        [DataMember]
        public DateTime Created { get; set; }
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessEvent.DataType"/>
        /// </summary>
        [DataMember]
        public Type DataType { get; set; }
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessEvent.Data"/>
        /// </summary>
        [DataMember]
        public string Data { get; set; }
        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IBusinessEvent.Token"/>
        /// </summary>
        [DataMember]
        public string Token { get; set; }

        #endregion

        #region [ Methods ]
        public object GetDataObject()
        {
            if (DataType.IsClass && DataType != typeof(string))
            {
                return Data.ToDeserialize(DataType);
            }
            return Convert.ChangeType(Data, DataType);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Created;
            yield return DataType;
            yield return Data;
            yield return Token;
        }
        #endregion
    }
}
