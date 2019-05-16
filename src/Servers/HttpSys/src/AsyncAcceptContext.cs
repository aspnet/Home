// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class AsyncAcceptContext : IValueTaskSource<NativeRequestContext>, IDisposable
    {
        internal static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(IOWaitCallback);
        private const int DefaultBufferSize = 4096;
        private const int AlignmentPadding = 8;

        private readonly HttpSysListener _server;
        private NativeRequestContext _nativeRequestContext;

        private NativeRequestContext _resultRequestContext;
        private Action<object> _continuation;
        private object _continuationState;
        private ValueTaskSourceStatus _status;
        private Exception _exception;


        internal AsyncAcceptContext(HttpSysListener server)
        {
            _server = server;
            AllocateNativeRequest();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by callback")]
        private static void IOCompleted(AsyncAcceptContext asyncResult, uint errorCode, uint numBytes)
        {
            bool complete = false;
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                    errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    asyncResult.TrySetException(new HttpSysException((int)errorCode));
                    complete = true;
                }
                else
                {
                    HttpSysListener server = asyncResult._server;
                    if (errorCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                    {
                        // at this point we have received an unmanaged HTTP_REQUEST and memoryBlob
                        // points to it we need to hook up our authentication handling code here.
                        try
                        {
                            if (server.ValidateRequest(asyncResult._nativeRequestContext) && server.ValidateAuth(asyncResult._nativeRequestContext))
                            {
                                asyncResult.TrySetResult();
                                complete = true;
                            }
                        }
                        catch (Exception)
                        {
                            server.SendError(asyncResult._nativeRequestContext.RequestId, StatusCodes.Status400BadRequest);
                            throw;
                        }
                        finally
                        {
                            // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                            if (complete)
                            {
                                asyncResult._nativeRequestContext = null;
                            }
                            else
                            {
                                asyncResult.AllocateNativeRequest(size: asyncResult._nativeRequestContext.Size);
                            }
                        }
                    }
                    else
                    {
                        //  (uint)backingBuffer.Length - AlignmentPadding
                       asyncResult.AllocateNativeRequest(numBytes, asyncResult._nativeRequestContext.RequestId);
                    }

                    // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                    if (!complete)
                    {
                        uint statusCode = asyncResult.QueueBeginGetContext();
                        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS &&
                            statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
                        {
                            // someother bad error, possible(?) return values are:
                            // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                            asyncResult.TrySetException(new HttpSysException((int)statusCode));
                            complete = true;
                        }
                    }
                    if (!complete)
                    {
                        return;
                    }
                }

                if (complete)
                {
                    asyncResult.Dispose();
                }
            }
            catch (Exception exception)
            {
                // Logged by caller
                asyncResult.TrySetException(exception);
                asyncResult.Dispose();
            }
        }

        private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            // take the ListenerAsyncResult object from the state
            var asyncResult = (AsyncAcceptContext)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        internal uint QueueBeginGetContext()
        {
            bool retry;
            uint statusCode;
            do
            {
                retry = false;
                uint bytesTransferred = 0;
                statusCode = HttpApi.HttpReceiveHttpRequest(
                    _server.RequestQueue.Handle,
                    _nativeRequestContext.RequestId,
                    (uint)HttpApiTypes.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                    _nativeRequestContext.NativeRequest,
                    _nativeRequestContext.Size,
                    &bytesTransferred,
                    _nativeRequestContext.NativeOverlapped);

                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_PARAMETER && _nativeRequestContext.RequestId != 0)
                {
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    _nativeRequestContext.RequestId = 0;
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_MORE_DATA)
                {
                    // the buffer was not big enough to fit the headers, we need
                    // to read the RequestId returned, allocate a new buffer of the required size
                    //  (uint)backingBuffer.Length - AlignmentPadding
                    AllocateNativeRequest(bytesTransferred);
                    retry = true;
                }
                else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS
                    && HttpSysListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    IOCompleted(this, statusCode, bytesTransferred);
                }
            }
            while (retry);
            return statusCode;
        }

        internal void AllocateNativeRequest(uint? size = null, ulong requestId = 0)
        {
            _nativeRequestContext?.ReleasePins();
            _nativeRequestContext?.Dispose();
            //Debug.Assert(size != 0, "unexpected size");

            // We can't reuse overlapped objects
            int newSize = checked((int)((size ?? DefaultBufferSize) + AlignmentPadding));
            var backingBuffer = ArrayPool<byte>.Shared.Rent(newSize);

            var boundHandle = _server.RequestQueue.BoundHandle;
            var nativeOverlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, backingBuffer));

            var requestAddress = Marshal.UnsafeAddrOfPinnedArrayElement(backingBuffer, 0);

            // TODO:
            // Apparently the HttpReceiveHttpRequest memory alignment requirements for non - ARM processors
            // are different than for ARM processors. We have seen 4 - byte - aligned buffers allocated on
            // virtual x64/x86 machines which were accepted by HttpReceiveHttpRequest without errors. In
            // these cases the buffer alignment may cause reading values at invalid offset. Setting buffer
            // alignment to 0 for now.
            // 
            // _bufferAlignment = (int)(requestAddress.ToInt64() & 0x07);

            var bufferAlignment = 0;

            var nativeRequest = (HttpApiTypes.HTTP_REQUEST*)(requestAddress + bufferAlignment);
            // nativeRequest
            _nativeRequestContext = new NativeRequestContext(nativeOverlapped, bufferAlignment, nativeRequest, backingBuffer, requestId);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_nativeRequestContext != null)
                {
                    _nativeRequestContext.ReleasePins();
                    _nativeRequestContext.Dispose();
                }
            }
        }

        private bool TrySetResult()
        {
            if (_status != ValueTaskSourceStatus.Pending)
            {
                return false;
            }

            _resultRequestContext = _nativeRequestContext;
            _status = ValueTaskSourceStatus.Succeeded;

            if (_continuation is object)
            {
                RunContinuation();
            }

            return true;
        }


        private bool TrySetException(Exception exception)
        {
            if (_status != ValueTaskSourceStatus.Pending)
            {
                return false;
            }

            _exception = exception;
            _status = ValueTaskSourceStatus.Faulted;

            if (_continuation is object)
            {
                RunContinuation();
            }

            return true;
        }

        private void RunContinuation()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_continuation, _continuationState, preferLocal: false);
            _continuation = null;
            _continuationState = null;
        }

        public ValueTask<NativeRequestContext> ValueTask
        {
            get
            {
                if (_status == ValueTaskSourceStatus.Succeeded)
                {
                    var resultRequestContext = _resultRequestContext;
                    _resultRequestContext = null;
                    return new ValueTask<NativeRequestContext>(resultRequestContext);
                }
                else
                {
                    return new ValueTask<NativeRequestContext>(this, short.MaxValue);
                }
            }
        }

        private static void ValidateToken(short token)
        {
            if (token != short.MaxValue)
            {
                throw new InvalidOperationException();
            }
        }

        NativeRequestContext IValueTaskSource<NativeRequestContext>.GetResult(short token)
        {
            ValidateToken(token);

            switch (_status)
            {
                case ValueTaskSourceStatus.Succeeded:
                    var resultRequestContext = _resultRequestContext;
                    _resultRequestContext = null;
                    return resultRequestContext;

                case ValueTaskSourceStatus.Faulted:
                    throw _exception;
                case ValueTaskSourceStatus.Canceled:
                    throw new TaskCanceledException();
                case ValueTaskSourceStatus.Pending:
                default:
                    throw new InvalidOperationException();
            }
        }

        ValueTaskSourceStatus IValueTaskSource<NativeRequestContext>.GetStatus(short token)
        {
            ValidateToken(token);
            return _status;
        }

        void IValueTaskSource<NativeRequestContext>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            ValidateToken(token);

            if (_continuation is object)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _continuationState = state;

            // Ignoring flags
        }
    }
}
