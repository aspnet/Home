// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A <see cref="IActionResultExecutor{ViewComponentResult}"/> for <see cref="ViewComponentResult"/>.
    /// </summary>
    public class ViewComponentResultExecutor : IActionResultExecutor<ViewComponentResult>
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly ILogger<ViewComponentResult> _logger;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private IHttpResponseStreamWriterFactory _writerFactory;

        /// <summary>
        /// Initialize a new instance of <see cref="ViewComponentResultExecutor"/>
        /// </summary>
        /// <param name="mvcHelperOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="tempDataDictionaryFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        /// <param name="writerFactory">The <see cref=" IHttpResponseStreamWriterFactory"/>.</param>
        public ViewComponentResultExecutor(
            IOptions<MvcViewOptions> mvcHelperOptions,
            ILoggerFactory loggerFactory,
            HtmlEncoder htmlEncoder,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IHttpResponseStreamWriterFactory writerFactory)
        {
            if (mvcHelperOptions == null)
            {
                throw new ArgumentNullException(nameof(mvcHelperOptions));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (tempDataDictionaryFactory == null)
            {
                throw new ArgumentNullException(nameof(tempDataDictionaryFactory));
            }

            _htmlHelperOptions = mvcHelperOptions.Value.HtmlHelperOptions;
            _logger = loggerFactory.CreateLogger<ViewComponentResult>();
            _htmlEncoder = htmlEncoder;
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
            _writerFactory = writerFactory;
        }

        /// <inheritdoc />
        public virtual async Task ExecuteAsync(ActionContext context, ViewComponentResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var response = context.HttpContext.Response;

            var viewData = result.ViewData;
            if (viewData == null)
            {
                viewData = new ViewDataDictionary(_modelMetadataProvider, context.ModelState);
            }

            var tempData = result.TempData;
            if (tempData == null)
            {
                tempData = _tempDataDictionaryFactory.GetTempData(context.HttpContext);
            }

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                result.ContentType,
                response.ContentType,
                ViewExecutor.DefaultContentType,
                out var resolvedContentType,
                out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            //_writerFactory ??= context.HttpContext.RequestServices.GetRequiredService<IHttpResponseStreamWriterFactory>();

            await using (var writer = _writerFactory != null
                ? _writerFactory.CreateWriter(response.Body, resolvedContentTypeEncoding)
                : new HttpResponsePipeWriter(response.BodyWriter, resolvedContentTypeEncoding))
            {
                var viewContext = new ViewContext(
                    context,
                    NullView.Instance,
                    viewData,
                    tempData,
                    writer,
                    _htmlHelperOptions);

                OnExecuting(viewContext);

                // IViewComponentHelper is stateful, we want to make sure to retrieve it every time we need it.
                var viewComponentHelper = context.HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
                (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);
                var viewComponentResult = await GetViewComponentResult(viewComponentHelper, _logger, result);

                if (viewComponentResult is ViewBuffer viewBuffer)
                {
                    // In the ordinary case, DefaultViewComponentHelper will return an instance of ViewBuffer. We can simply
                    // invoke WriteToAsync on it.
                    await viewBuffer.WriteToAsync(writer, _htmlEncoder);
                    await writer.FlushAsync();
                }
                else
                {
                    await using var bufferingStream = new FileBufferingPipeWriter(response.BodyWriter);
                    await using (var intermediateWriter = new HttpResponsePipeWriter(response.BodyWriter, resolvedContentTypeEncoding))
                    {
                        viewComponentResult.WriteTo(intermediateWriter, _htmlEncoder);
                    }

                    await bufferingStream.DrainBufferAsync();
                }
            }
        }

        private void OnExecuting(ViewContext viewContext)
        {
            var viewDataValuesProvider = viewContext.HttpContext.Features.Get<IViewDataValuesProviderFeature>();
            if (viewDataValuesProvider != null)
            {
                viewDataValuesProvider.ProvideViewDataValues(viewContext.ViewData);
            }
        }

        private Task<IHtmlContent> GetViewComponentResult(IViewComponentHelper viewComponentHelper, ILogger logger, ViewComponentResult result)
        {
            if (result.ViewComponentType == null && result.ViewComponentName == null)
            {
                throw new InvalidOperationException(Resources.FormatViewComponentResult_NameOrTypeMustBeSet(
                    nameof(ViewComponentResult.ViewComponentName),
                    nameof(ViewComponentResult.ViewComponentType)));
            }
            else if (result.ViewComponentType == null)
            {
                logger.ViewComponentResultExecuting(result.ViewComponentName);
                return viewComponentHelper.InvokeAsync(result.ViewComponentName, result.Arguments);
            }
            else
            {
                logger.ViewComponentResultExecuting(result.ViewComponentType);
                return viewComponentHelper.InvokeAsync(result.ViewComponentType, result.Arguments);
            }
        }
    }
}
