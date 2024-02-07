//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Application.Pipe;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Resolvers
{
    /// <summary>
    /// 
    /// </summary>
    public class PipelineBuilderResolver
    {
        private readonly Dictionary<string, Type> _builders;
        private readonly Dictionary<string, List<Type>> _buildersComplex;

        /// <summary>
        /// 
        /// </summary>
        public PipelineBuilderResolver()
        {
            _builders = [];
            _buildersComplex = [];
        }

        /// <summary>
        /// 
        /// </summary>
        public PipelineBuilderResolver AddList<T, U>()
            where T : IPipelineBuilder
            where U : IPipelineBuilder, new()
        {
            return AddList<T, U>(typeof(T).FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        public PipelineBuilderResolver AddList<T, U>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
            where U : IPipelineBuilder, new()
        {
            string keyName = key;
            if (!isSimpleKey)
            {
                keyName = $"{key}_{typeof(T).FullName}";
            }

            if (!_buildersComplex.TryGetValue(keyName, out List<Type> value))
            {
                value = [];
                _buildersComplex.Add(keyName, value);
            }

            value.Add(typeof(U));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public PipelineBuilderResolver Add<T, U>()
            where T : IPipelineBuilder
            where U : IPipelineBuilder, new()
        {
            Add<T, U>(typeof(T).FullName);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public PipelineBuilderResolver Add<T, U>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
            where U : IPipelineBuilder, new()
        {
            string keyName = key;
            if (!isSimpleKey)
            {
                keyName = $"{key}_{typeof(T).FullName}";
            }

            if (_builders.ContainsKey(keyName))
            {
                _builders[keyName] = typeof(U);
            }
            else
            {
                _builders.Add(keyName, typeof(U));
            }

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public T Get<T>()
            where T : IPipelineBuilder
        {
            return Get<T>(typeof(T).FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        public T Get<T>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
        {
            string keyName = key;
            if (!isSimpleKey)
            {
                keyName = $"{key}_{typeof(T).FullName}";
            }

            if (_builders.TryGetValue(keyName, out Type value))
            {
                return (T)Activator.CreateInstance(value);
            }
            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<T> GetList<T>()
            where T : IPipelineBuilder
        {
            return GetList<T>(typeof(T).FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<T> GetList<T>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
        {
            string keyName = key;
            if (!isSimpleKey)
            {
                keyName = $"{key}_{typeof(T).FullName}";
            }

            if (_buildersComplex.TryGetValue(keyName, out List<Type> value))
            {
                var result = new List<T>();
                foreach (var item in value)
                {
                    result.Add((T)Activator.CreateInstance(item));
                }

                return result;
            }
            return [];
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>()
            where T : IPipelineBuilder
        {
            return Has(typeof(T).FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
        {
            return Has(key, isSimpleKey ? null : typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has(string key, Type typeComposeKey = null)
        {
            string keyName = key;
            if (typeComposeKey != null)
            {
                keyName = $"{key}_{typeComposeKey.FullName}";
            }

            return _builders.ContainsKey(keyName);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasList<T>(string key, bool isSimpleKey = false)
            where T : IPipelineBuilder
        {
            return HasList(key, isSimpleKey ? null : typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasList(string key, Type typeComposeKey = null)
        {
            string keyName = key;
            if (typeComposeKey != null)
            {
                keyName = $"{key}_{typeComposeKey.FullName}";
            }

            return _buildersComplex.ContainsKey(keyName);
        }
    }
}
