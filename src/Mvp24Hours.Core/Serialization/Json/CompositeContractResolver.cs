using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Core.Serialization.Json
{
    /// <summary>
    /// <see href="https://gist.github.com/TheMulti0/cd2b1a40677fd74480958b363e206388"/>
    /// </summary>
    /// <remarks>
    /// <code>
    ///  new JsonSerializerSettings
    ///  {
    ///      ContractResolver = new CompositeContractResolver
    ///      {
    ///          new InterfaceContractResolver<ISomething>(),
    ///          new DefaultContractResolver()
    ///      }
    ///  }
    /// </code>
    /// </remarks>
    public class CompositeContractResolver : IContractResolver, IEnumerable<IContractResolver>
    {
        private readonly IList<IContractResolver> _contractResolvers = new List<IContractResolver>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public JsonContract ResolveContract(Type type)
        {
            return
                _contractResolvers
                    .Select(x => x.ResolveContract(type))
                    .FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractResolver"></param>
        public void Add(IContractResolver contractResolver)
        {
            ArgumentNullException.ThrowIfNull(contractResolver);
            _contractResolvers.Add(contractResolver);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IContractResolver> GetEnumerator()
        {
            return _contractResolvers.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
