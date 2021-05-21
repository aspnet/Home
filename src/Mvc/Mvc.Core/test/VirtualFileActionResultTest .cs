// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class VirtualFileActionResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");

            // Act
            var result = new VirtualFileResult(path, "text/plain");

            // Assert
            Assert.Equal(path, result.FileName);
        }

        [Fact]
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new VirtualFileResult(path, contentType);

            // Assert
            Assert.Equal(path, result.FileName);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 4, "ts¡", 4)]
        [InlineData(8, null, "ResultTestFile contents¡", 25)]
        public async Task WriteFileAsync_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.WriteFileAsync_WritesRangeRequested(
                start,
                end,
                expectedString,
                contentLength,
                actionType,
                action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(actionType, action);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(string rangeString)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(rangeString, actionType, action);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_RangeRequestedNotSatisfiable(string rangeString)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .WriteFileAsync_RangeRequestedNotSatisfiable(rangeString, actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.WriteFileAsync_RangeRequested_PreconditionFailed(actionType, action);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_NotModified()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.WriteFileAsync_RangeRequested_NotModified(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_FallsBackToWebRootFileProvider_IfNoFileProviderIsPresent()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .ExecuteResultAsync_FallsBackToWebRootFileProvider_IfNoFileProviderIsPresent(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent(actionType, action);
        }

        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 3, "ts¡", 3)]
        [InlineData(8, null, "ResultTestFile contents¡", 25)]
        public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, string expectedString, long contentLength)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(
                start,
                end,
                expectedString,
                contentLength,
                actionType,
                action);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_ReturnsFileContentsForRelativePaths()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.ExecuteResultAsync_ReturnsFileContentsForRelativePaths(actionType, action);
        }

        [Theory]
        [InlineData("FilePathResultTestFile.txt")]
        [InlineData("TestFiles/FilePathResultTestFile.txt")]
        [InlineData("TestFiles/../FilePathResultTestFile.txt")]
        [InlineData("TestFiles\\FilePathResultTestFile.txt")]
        [InlineData("TestFiles\\..\\FilePathResultTestFile.txt")]
        [InlineData(@"\\..//?><|""&@#\c:\..\? /..txt")]
        public async Task ExecuteResultAsync_ReturnsFiles_ForDifferentPaths(string path)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .ExecuteResultAsync_ReturnsFiles_ForDifferentPaths(path, actionType, action);
        }

        [Theory]
        [InlineData("~/FilePathResultTestFile.txt")]
        [InlineData("~/TestFiles/FilePathResultTestFile.txt")]
        [InlineData("~/TestFiles/../FilePathResultTestFile.txt")]
        [InlineData("~/TestFiles\\..\\FilePathResultTestFile.txt")]
        [InlineData(@"~~~~\\..//?>~<|""&@#\c:\..\? /..txt~~~")]
        public async Task ExecuteResultAsync_TrimsTilde_BeforeInvokingFileProvider(string path)
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest
                .ExecuteResultAsync_TrimsTilde_BeforeInvokingFileProvider(path, actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithNonDiskBasedFiles()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.ExecuteResultAsync_WorksWithNonDiskBasedFiles(actionType, action);
        }

        [Fact]
        public async Task ExecuteResultAsync_ThrowsFileNotFound_IfFileProviderCanNotFindTheFile()
        {
            var actionType = "ActionContext";
            var action = new Func<VirtualFileResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context));

            await BaseVirtualFileResultTest.ExecuteResultAsync_ThrowsFileNotFound_IfFileProviderCanNotFindTheFile(actionType, action);
        }
    }
}