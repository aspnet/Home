// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Abstract base class for an in-process JavaScript runtime.
    /// </summary>
    public abstract class JSInProcessRuntime : JSRuntime, IJSInProcessRuntime
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JSInProcessRuntime"/>.
        /// </summary>
        protected JSInProcessRuntime()
        {
            JsonSerializerOptions.Converters.Add(
                new JSObjectReferenceJsonConverter<IJSInProcessObjectReference, JSInProcessObjectReference>(
                    id => new JSInProcessObjectReference(this, id)));
        }

        internal TValue Invoke<TValue>(string identifier, long targetInstanceId, JsonSerializerOptions jsonSerializerOptions, params object?[]? args)
        {
            var resultJson = InvokeJS(
                identifier,
                JsonSerializer.Serialize(args, jsonSerializerOptions),
                JSCallResultTypeHelper.FromGeneric<TValue>(),
                targetInstanceId);

            // While the result of deserialization could be null, we're making a
            // quality of life decision and letting users explicitly determine if they expect
            // null by specifying TValue? as the expected return type.
            if (resultJson is null)
            {
                return default!;
            }

            return JsonSerializer.Deserialize<TValue>(resultJson, jsonSerializerOptions)!;
        }

        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        public TValue Invoke<TValue>(string identifier, params object?[]? args)
            => Invoke<TValue>(identifier, 0, JsonSerializerOptions, args);

        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="jsonSerializerOptions">JSON serialization options to use during serialization/deserialization of the args and return value.</param> 
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
        public TResult Invoke<TResult>(string identifier, JsonSerializerOptions jsonSerializerOptions, params object?[]? args)
            => Invoke<TResult>(identifier, 0, jsonSerializerOptions, args);

        /// <summary>
        /// Performs a synchronous function invocation.
        /// </summary>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        /// <returns>A JSON representation of the result.</returns>
        protected virtual string? InvokeJS(string identifier, string? argsJson)
            => InvokeJS(identifier, argsJson, JSCallResultType.Default, 0);

        /// <summary>
        /// Performs a synchronous function invocation.
        /// </summary>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        /// <param name="resultType">The type of result expected from the invocation.</param>
        /// <param name="targetInstanceId">The instance ID of the target JS object.</param>
        /// <returns>A JSON representation of the result.</returns>
        protected abstract string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId);
    }
}
