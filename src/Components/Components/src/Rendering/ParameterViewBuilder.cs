// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Provides a mechanism for building a <see cref="ParameterView" />.
    /// </summary>
    public sealed class ParameterViewBuilder : IDisposable
    {
        private const string GeneratedParameterViewElementName = "__ARTIFICIAL_PARAMETER_VIEW";

        internal ArrayBuilder<RenderTreeFrame> ReferenceFramesBuffer { get; } = new ArrayBuilder<RenderTreeFrame>(64);

        /// <summary>
        /// Constructs an instance of <see cref="ParameterViewBuilder"/>.
        /// </summary>
        public ParameterViewBuilder()
        {
            Clear();
        }

        /// <summary>
        /// Clears the instance so that it can be used to build a new <see cref="ParameterView" />.
        /// </summary>
        public void Clear()
        {
            // TODO: Invalidate the parameterview lifetime
            ReferenceFramesBuffer.Clear();
            ReferenceFramesBuffer.Append(RenderTreeFrame.Element(0, GeneratedParameterViewElementName));
        }

        /// <summary>
        /// Adds a parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public void Add(string name, object? value)
        {
            // TODO: Invalidate the parameterview lifetime
            ReferenceFramesBuffer.Append(RenderTreeFrame.Attribute(1, name, value));
        }

        /// <summary>
        /// Supplies a <see cref="ParameterView" /> containing the parameters added to this instance
        /// since it was last cleared.
        /// </summary>
        /// <returns>The <see cref="ParameterView" />.</returns>
        public ParameterView ToParameterView()
        {
            // TODO: Don't use ParameterViewLifetime.Unbound
            ReferenceFramesBuffer.Buffer[0].ElementSubtreeLengthField = ReferenceFramesBuffer.Count;
            return new ParameterView(ParameterViewLifetime.Unbound,
                ReferenceFramesBuffer.Buffer, 0);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ReferenceFramesBuffer.Dispose();
        }
    }
}
