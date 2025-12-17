//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Pipe.ExceptionMapping
{
    /// <summary>
    /// Default implementation of exception mapper with configurable rules.
    /// </summary>
    public class DefaultPipelineExceptionMapper : IPipelineExceptionMapper
    {
        private readonly List<ExceptionMappingRule> _rules = new();
        private Func<Exception, IEnumerable<IMessageResult>>? _defaultMapper;
        private Func<Exception, bool>? _defaultShouldFail;
        private Func<Exception, bool>? _defaultShouldPropagate;

        /// <summary>
        /// Creates a new instance with default behavior.
        /// </summary>
        public DefaultPipelineExceptionMapper()
        {
            _defaultMapper = ex => new[]
            {
                new MessageResult((ex.InnerException ?? ex).Message, MessageType.Error)
            };
            _defaultShouldFail = _ => true;
            _defaultShouldPropagate = _ => false;
        }

        /// <summary>
        /// Adds a mapping rule for a specific exception type.
        /// </summary>
        /// <typeparam name="TException">Type of exception to handle.</typeparam>
        /// <param name="mapper">Function to map the exception to message results.</param>
        /// <param name="shouldFail">Whether this exception should mark the pipeline as faulty.</param>
        /// <param name="shouldPropagate">Whether this exception should be rethrown.</param>
        /// <returns>This mapper for chaining.</returns>
        public DefaultPipelineExceptionMapper AddRule<TException>(
            Func<TException, IEnumerable<IMessageResult>> mapper,
            bool shouldFail = true,
            bool shouldPropagate = false) where TException : Exception
        {
            _rules.Add(new ExceptionMappingRule(
                typeof(TException),
                ex => mapper((TException)ex),
                shouldFail,
                shouldPropagate));
            return this;
        }

        /// <summary>
        /// Sets the default mapper for unhandled exception types.
        /// </summary>
        /// <param name="mapper">Default mapping function.</param>
        /// <returns>This mapper for chaining.</returns>
        public DefaultPipelineExceptionMapper SetDefaultMapper(Func<Exception, IEnumerable<IMessageResult>> mapper)
        {
            _defaultMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            return this;
        }

        /// <summary>
        /// Sets the default behavior for determining if exceptions should fail the pipeline.
        /// </summary>
        /// <param name="shouldFail">Default function to determine failure.</param>
        /// <returns>This mapper for chaining.</returns>
        public DefaultPipelineExceptionMapper SetDefaultShouldFail(Func<Exception, bool> shouldFail)
        {
            _defaultShouldFail = shouldFail ?? throw new ArgumentNullException(nameof(shouldFail));
            return this;
        }

        /// <summary>
        /// Sets the default behavior for determining if exceptions should propagate.
        /// </summary>
        /// <param name="shouldPropagate">Default function to determine propagation.</param>
        /// <returns>This mapper for chaining.</returns>
        public DefaultPipelineExceptionMapper SetDefaultShouldPropagate(Func<Exception, bool> shouldPropagate)
        {
            _defaultShouldPropagate = shouldPropagate ?? throw new ArgumentNullException(nameof(shouldPropagate));
            return this;
        }

        /// <inheritdoc />
        public IEnumerable<IMessageResult> Map(Exception exception)
        {
            var rule = FindRule(exception);
            if (rule != null)
            {
                return rule.Mapper(exception);
            }
            return _defaultMapper?.Invoke(exception) ?? Enumerable.Empty<IMessageResult>();
        }

        /// <inheritdoc />
        public bool ShouldFail(Exception exception)
        {
            var rule = FindRule(exception);
            if (rule != null)
            {
                return rule.ShouldFail;
            }
            return _defaultShouldFail?.Invoke(exception) ?? true;
        }

        /// <inheritdoc />
        public bool ShouldPropagate(Exception exception)
        {
            var rule = FindRule(exception);
            if (rule != null)
            {
                return rule.ShouldPropagate;
            }
            return _defaultShouldPropagate?.Invoke(exception) ?? false;
        }

        private ExceptionMappingRule? FindRule(Exception exception)
        {
            // Find the most specific matching rule
            return _rules
                .Where(r => r.ExceptionType.IsInstanceOfType(exception))
                .OrderByDescending(r => GetTypeHierarchyDepth(r.ExceptionType))
                .FirstOrDefault();
        }

        private static int GetTypeHierarchyDepth(Type type)
        {
            int depth = 0;
            var current = type;
            while (current != null)
            {
                depth++;
                current = current.BaseType;
            }
            return depth;
        }

        private record ExceptionMappingRule(
            Type ExceptionType,
            Func<Exception, IEnumerable<IMessageResult>> Mapper,
            bool ShouldFail,
            bool ShouldPropagate);
    }
}

