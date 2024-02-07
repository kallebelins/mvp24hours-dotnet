//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Mvp24Hours.Extensions
{
    public static class ValidatorEntityExtensions
    {
        public static IList<IMessageResult> TryValidate<TEntity>(this TEntity entity, IValidator<TEntity> _validator = null)
            where TEntity : class
        {
            var validator = _validator;
            if (validator != null)
            {
                var validationResult = validator.Validate(entity);
                if (!validationResult.IsValid)
                {
                    return validationResult.Errors
                        .Select(x => (IMessageResult)new MessageResult(x.ErrorCode, x.ErrorMessage, Core.Enums.MessageType.Error))
                        .ToList();
                }
            }
            else
            {
                var validationRslts = new List<ValidationResult>();
                var validationCntxt = new ValidationContext(entity, null, null);
                if (!Validator.TryValidateObject(entity, validationCntxt, validationRslts, true))
                {
                    return validationRslts
                        .Select(item => (IMessageResult)new MessageResult(string.Join("|", item.MemberNames), item.ErrorMessage, Core.Enums.MessageType.Error))
                        .ToList();
                }
            }
            return new List<IMessageResult>();
        }
    }
}
