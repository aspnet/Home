// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web
{
    // This type is a way of exposing some of the renderer methods to JS interop without
    // otherwise changing the public API surface.

    /// <summary>
    /// Contains methods called by interop. Intended for framework use only, not supported for use in application
    /// code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DynamicRootComponentInterop : IDisposable
    {
        private readonly DotNetObjectReference<DynamicRootComponentInterop> _selfReference;
        private readonly IJSRuntime _jsRuntime;
        private readonly Func<Type, string, int> _addRootComponent;
        private readonly Func<int, ParameterView, Task> _renderRootComponentAsync;
        private readonly Action<int> _removeRootComponent;
        private readonly Dictionary<string, Type> _allowedComponentTypes = new(StringComparer.Ordinal);
        private readonly ParameterViewBuilder _parameterViewBuilder = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <param name="jsRuntime"></param>
        /// <param name="addRootComponent"></param>
        /// <param name="renderRootComponentAsync"></param>
        /// <param name="removeRootComponent"></param>
        public DynamicRootComponentInterop(
            DynamicRootComponentConfiguration configurationBuilder,
            IJSRuntime jsRuntime,
            Func<Type, string, int> addRootComponent,
            Func<int, ParameterView, Task> renderRootComponentAsync,
            Action<int> removeRootComponent)
        {
            _selfReference = DotNetObjectReference.Create(this);
            _jsRuntime = jsRuntime;
            _addRootComponent = addRootComponent;
            _removeRootComponent = removeRootComponent;
            _renderRootComponentAsync = renderRootComponentAsync;

            foreach (var entry in configurationBuilder.AllowedComponents)
            {
                _allowedComponentTypes.Add(entry.Identifier, entry.ComponentType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async ValueTask InitializeAsync()
        {
            if (_allowedComponentTypes.Count > 0)
            {
                await _jsRuntime.InvokeVoidAsync(
                    "Blazor._internal.setDynamicRootComponentManager",
                    _selfReference);
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public int AddRootComponent(string componentIdentifier, string domElementSelector)
        {
            if (!_allowedComponentTypes.TryGetValue(componentIdentifier, out var componentType))
            {
                throw new ArgumentException($"There is no registered dynamic root component with identifier '{componentIdentifier}'.");
            }

            return _addRootComponent(componentType, domElementSelector);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public Task RenderRootComponentAsync(int componentId, JsonElement parameters)
        {
            try
            {
                // TODO: Use the [Parameter] attributes on the component type to create a precached
                // JsonElement->ParameterView parser that respects the declared parameter types, not
                // the actual types in the JSON data.
                foreach (var jsonElement in parameters.EnumerateObject())
                {
                    switch (jsonElement.Value.ValueKind)
                    {
                        case JsonValueKind.Number:
                            _parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetInt32());
                            break;
                        case JsonValueKind.String:
                            _parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetString());
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            _parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetBoolean());
                            break;
                    }
                }

                return _renderRootComponentAsync(componentId, _parameterViewBuilder.ToParameterView());
            }
            finally
            {
                _parameterViewBuilder.Clear();
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void RemoveRootComponent(int componentId)
            => _removeRootComponent(componentId);

        /// <inheritdoc />
        public void Dispose()
        {
            _parameterViewBuilder.Dispose();
            _selfReference.Dispose();
        }
    }
}
