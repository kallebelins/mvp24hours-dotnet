//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Exceptions
{
    /// <summary>
    /// Composite mapper that delegates to specialized mappers based on exception type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mapper uses the Chain of Responsibility pattern to delegate mapping
    /// to specialized mappers. The first mapper that can handle the exception
    /// is used to produce the ProblemDetails response.
    /// </para>
    /// <para>
    /// If no specialized mapper can handle the exception, the default mapper is used.
    /// </para>
    /// </remarks>
    public class CompositeExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
    {
        private readonly IEnumerable<IExceptionToProblemDetailsMapper> _mappers;
        private readonly IExceptionToProblemDetailsMapper _defaultMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeExceptionToProblemDetailsMapper"/> class.
        /// </summary>
        /// <param name="mappers">The collection of specialized mappers.</param>
        /// <param name="defaultMapper">The default mapper to use when no specialized mapper can handle the exception.</param>
        public CompositeExceptionToProblemDetailsMapper(
            IEnumerable<IExceptionToProblemDetailsMapper> mappers,
            DefaultExceptionToProblemDetailsMapper defaultMapper)
        {
            _mappers = mappers ?? throw new ArgumentNullException(nameof(mappers));
            _defaultMapper = defaultMapper ?? throw new ArgumentNullException(nameof(defaultMapper));
        }

        /// <inheritdoc />
        public bool CanHandle(Exception exception) => true;

        /// <inheritdoc />
        public int GetStatusCode(Exception exception)
        {
            foreach (var mapper in _mappers)
            {
                if (mapper != this && mapper.CanHandle(exception))
                {
                    return mapper.GetStatusCode(exception);
                }
            }

            return _defaultMapper.GetStatusCode(exception);
        }

        /// <inheritdoc />
        public ProblemDetails Map(Exception exception, HttpContext context)
        {
            foreach (var mapper in _mappers)
            {
                if (mapper != this && mapper.CanHandle(exception))
                {
                    return mapper.Map(exception, context);
                }
            }

            return _defaultMapper.Map(exception, context);
        }
    }
}

