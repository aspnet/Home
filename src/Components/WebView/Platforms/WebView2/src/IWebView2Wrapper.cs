using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Provides an abstraction for different UI frameworks to provide access to APIs from
    /// <see cref="Microsoft.Web.WebView2.Core.CoreWebView2"/> and related controls.
    /// </summary>
    public interface IWebView2Wrapper
    {
        /// <summary>
        /// Gets the <see cref="CoreWebView2"/> instance on the control. This is only available
        /// once the <see cref="Task"/> returned by <see cref="EnsureCoreWebView2Async(CoreWebView2Environment)"/>
        /// has completed.
        /// </summary>
        CoreWebView2 CoreWebView2 { get; }

        /// <summary>
        /// Gets or sets the source URI of the control. Setting the source URI causes page navigation.
        /// </summary>
        Uri Source { get; set; }

        /// <summary>
        /// Initializes the <see cref="CoreWebView2"/> instance on the control. This should only be called once
        /// per control.
        /// </summary>
        /// <param name="environment">A <see cref="CoreWebView2Environment"/> that can be used to customize the control's behavior.</param>
        /// <returns>A <see cref="Task"/> that will complete once the <see cref="CoreWebView2"/> is initialized and attached to the control.</returns>
        Task EnsureCoreWebView2Async(CoreWebView2Environment environment = null);
    }
}