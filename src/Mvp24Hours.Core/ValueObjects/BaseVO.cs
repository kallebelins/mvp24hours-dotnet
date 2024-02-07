//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Base value object
    /// </summary>
    public abstract class BaseVO
    {
        #region [ Equality ]
        /// <summary>
        /// Gets value that defines the object instance
        /// </summary>
        protected abstract IEnumerable<object> GetEqualityComponents();
        #endregion

        #region [ Overrides ]

        /// <summary>
        /// <see cref="System.Object.Equals(object?)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var valueObject = (BaseVO)obj;
            return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
        }
        /// <summary>
        /// <see cref="System.Object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(1, (current, obj) =>
                {
                    return HashCode.Combine(current, obj);
                });
        }
        /// <summary>
        /// Equality comparator
        /// </summary>
#pragma warning disable S3875 // "operator==" should not be overloaded on reference types
        public static bool operator ==(BaseVO a, BaseVO b)
#pragma warning restore S3875 // "operator==" should not be overloaded on reference types
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Inequality comparator
        /// </summary>
        public static bool operator !=(BaseVO a, BaseVO b)
        {
            return !(a == b);
        }

        #endregion
    }
}
