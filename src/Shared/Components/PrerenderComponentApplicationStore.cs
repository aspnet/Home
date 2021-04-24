// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components
{
    internal class PrerenderComponentApplicationStore : IPersistentComponentStateStore
    {
#nullable enable
        private char[]? _buffer;
#nullable disable

        public PrerenderComponentApplicationStore()
        {
            ExistingState = new();
        }

        public PrerenderComponentApplicationStore(string existingState)
        {
            if (existingState is null)
            {
                throw new ArgumentNullException(nameof(existingState));
            }

            DeserializeState(Convert.FromBase64String(existingState));
        }

        protected void DeserializeState(byte[] existingState)
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(existingState);
            if (state == null)
            {
                throw new ArgumentException("Could not deserialize state correctly", nameof(existingState));
            }

            var stateDictionary = new Dictionary<string, ReadOnlySequence<byte>>();
            foreach (var (key, value) in state)
            {
                stateDictionary.Add(key, new ReadOnlySequence<byte>(value));
            }

            ExistingState = stateDictionary;
        }

#nullable enable
        public ReadOnlyMemory<char> PersistedState { get; private set; }
#nullable disable

        public Dictionary<string, ReadOnlySequence<byte>> ExistingState { get; protected set; }

        public Task<IDictionary<string, ReadOnlySequence<byte>>> GetPersistedStateAsync()
        {
            return Task.FromResult((IDictionary<string, ReadOnlySequence<byte>>)ExistingState);
        }

        protected virtual PooledByteBufferWriter SerializeState(IReadOnlyDictionary<string, ReadOnlySequence<byte>> state)
        {
            // System.Text.Json doesn't support serializing ReadonlySequence<byte> so we need to buffer
            // the data with a memory pool here. We will change our serialization strategy in the future here
            // so that we can avoid this step.
            var buffer = new PooledByteBufferWriter();
            try
            {
                var jsonWriter = new Utf8JsonWriter(buffer);
                jsonWriter.WriteStartObject();
                foreach (var (key, value) in state)
                {
                    if (value.IsSingleSegment)
                    {
                        jsonWriter.WriteBase64String(key, value.First.Span);
                    }
                    else
                    {
                        WriteMultipleSegments(jsonWriter, key, value);
                    }
                    jsonWriter.Flush();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.Flush();
                return buffer;

            }
            catch
            {
                buffer.Dispose();
                throw;
            }
            static void WriteMultipleSegments(Utf8JsonWriter jsonWriter, string key, ReadOnlySequence<byte> value)
            {
                byte[] unescapedArray = null;
                var valueLength = (int)value.Length;

                Span<byte> valueSegment = value.Length <= 256 ?
                    stackalloc byte[valueLength] :
                    (unescapedArray = ArrayPool<byte>.Shared.Rent(valueLength)).AsSpan().Slice(0, valueLength);

                value.CopyTo(valueSegment);
                jsonWriter.WriteBase64String(key, valueSegment);

                if (unescapedArray != null)
                {
                    valueSegment.Clear();
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, ReadOnlySequence<byte>> state)
        {
            using var bytes = SerializeState(state);
            var length = Base64.GetMaxEncodedToUtf8Length(bytes.WrittenCount);
            // We can do this because the representation in bytes for characters in the base64 alphabet for utf-8 is 1.
            _buffer = ArrayPool<char>.Shared.Rent(length);

            var memory = _buffer.AsMemory().Slice(0, length);
            Convert.TryToBase64Chars(bytes.WrittenMemory.Span, memory.Span, out _);
            PersistedState = memory;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<char>.Shared.Return(_buffer, true);
                _buffer = null;
            }
        }
    }
}
