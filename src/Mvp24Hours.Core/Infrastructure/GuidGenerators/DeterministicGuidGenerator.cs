//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Infrastructure.GuidGenerators
{
    /// <summary>
    /// GUID generator for testing that produces predictable GUIDs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this generator in unit tests to have predictable GUID values.
    /// You can either:
    /// - Provide a sequence of GUIDs to return
    /// - Let it generate sequential GUIDs (1, 2, 3, etc.)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with predefined GUIDs
    /// var expectedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    /// var generator = new DeterministicGuidGenerator(expectedId);
    /// var service = new OrderService(generator);
    /// var order = service.CreateOrder();
    /// Assert.Equal(expectedId, order.Id);
    /// 
    /// // Using with sequential GUIDs
    /// var generator = new DeterministicGuidGenerator();
    /// var first = generator.NewGuid();  // 00000000-0000-0000-0000-000000000001
    /// var second = generator.NewGuid(); // 00000000-0000-0000-0000-000000000002
    /// </code>
    /// </example>
    public sealed class DeterministicGuidGenerator : IGuidGenerator
    {
        private readonly Queue<Guid> _predefinedGuids;
        private int _counter;
        private readonly bool _useSequential;

        /// <summary>
        /// Creates a generator that produces sequential GUIDs starting from 1.
        /// </summary>
        public DeterministicGuidGenerator()
        {
            _predefinedGuids = new Queue<Guid>();
            _counter = 0;
            _useSequential = true;
        }

        /// <summary>
        /// Creates a generator that returns the specified GUIDs in order.
        /// </summary>
        /// <param name="guids">The GUIDs to return, in order.</param>
        public DeterministicGuidGenerator(params Guid[] guids)
        {
            _predefinedGuids = new Queue<Guid>(guids);
            _useSequential = false;
        }

        /// <summary>
        /// Creates a generator that returns the specified GUIDs in order.
        /// </summary>
        /// <param name="guids">The GUIDs to return, in order.</param>
        public DeterministicGuidGenerator(IEnumerable<Guid> guids)
        {
            _predefinedGuids = new Queue<Guid>(guids);
            _useSequential = false;
        }

        /// <inheritdoc />
        public Guid NewGuid()
        {
            if (_useSequential)
            {
                _counter++;
                return CreateSequentialGuid(_counter);
            }

            if (_predefinedGuids.Count == 0)
            {
                throw new InvalidOperationException(
                    "No more predefined GUIDs available. " +
                    "Add more GUIDs or use the sequential generator constructor.");
            }

            return _predefinedGuids.Dequeue();
        }

        /// <summary>
        /// Adds more GUIDs to the queue.
        /// </summary>
        /// <param name="guids">The GUIDs to add.</param>
        public void AddGuids(params Guid[] guids)
        {
            foreach (var guid in guids)
            {
                _predefinedGuids.Enqueue(guid);
            }
        }

        /// <summary>
        /// Gets the number of GUIDs remaining in the queue.
        /// </summary>
        public int RemainingCount => _predefinedGuids.Count;

        /// <summary>
        /// Resets the sequential counter to 0.
        /// </summary>
        public void Reset()
        {
            _counter = 0;
            _predefinedGuids.Clear();
        }

        /// <summary>
        /// Creates a GUID from a sequential number.
        /// </summary>
        private static Guid CreateSequentialGuid(int number)
        {
            // Create a predictable GUID format: 00000000-0000-0000-0000-{number:D12}
            var bytes = new byte[16];
            var numberBytes = BitConverter.GetBytes(number);
            
            // Place the number in the last 4 bytes
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberBytes);
            }
            
            Buffer.BlockCopy(numberBytes, 0, bytes, 12, 4);
            
            return new Guid(bytes);
        }

        /// <summary>
        /// Creates a well-known GUID from a simple integer for easy identification in tests.
        /// </summary>
        /// <param name="number">The number to convert (1-999999999999).</param>
        /// <returns>A GUID in the format 00000000-0000-0000-0000-{number:D12}.</returns>
        public static Guid FromNumber(int number)
        {
            return CreateSequentialGuid(number);
        }

        /// <summary>
        /// Creates a well-known GUID from a seed string.
        /// The same seed will always produce the same GUID.
        /// </summary>
        /// <param name="seed">The seed string.</param>
        /// <returns>A deterministic GUID based on the seed.</returns>
        public static Guid FromSeed(string seed)
        {
            if (string.IsNullOrEmpty(seed))
            {
                throw new ArgumentNullException(nameof(seed));
            }

            // Use a simple hash to generate a deterministic GUID
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(seed);
            var hashBytes = md5.ComputeHash(inputBytes);
            return new Guid(hashBytes);
        }
    }
}

