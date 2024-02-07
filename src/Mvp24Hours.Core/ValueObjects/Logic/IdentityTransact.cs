//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// Maintains identity during distributed transaction process.
    /// </summary>
    public class IdentityTransact : BaseVO
    {
        private Guid? _id;

        public virtual Guid? Id
        {
            get
            {
                if (_id == null || _id == Guid.Empty)
                {
                    _id = Guid.NewGuid();
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
        }
    }
}
