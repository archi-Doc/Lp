// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Netsphere.Packet;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace Netsphere;

public static class StreamHelper
{
    private const int BufferSize = 1024 * 1024 * 4;

    public static async Task<NetResult> ReceiveStreamToStream(ReceiveStream receiveStream, Stream stream, CancellationToken cancellationToken = default)
    {
        var result = NetResult.UnknownError;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (true)
            {
                (result, var written) = await receiveStream.Receive(buffer, cancellationToken).ConfigureAwait(false);
                if (written > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
                }

                if (result == NetResult.Success)
                {// Continue
                }
                else if (result == NetResult.Completed)
                {// Completed
                    result = NetResult.Success;
                    break;
                }
                else
                {// Error
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return result;
    }

    public static async Task<NetResult> ReceiveStreamToStream<TResponse>(ReceiveStream<TResponse> receiveStream, Stream stream, CancellationToken cancellationToken = default)
    {
        var result = NetResult.UnknownError;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (true)
            {
                (result, var written) = await receiveStream.Receive(buffer, cancellationToken).ConfigureAwait(false);
                if (written > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
                }

                if (result == NetResult.Success)
                {// Continue
                }
                else if (result == NetResult.Completed)
                {// Completed
                    result = NetResult.Success;
                    break;
                }
                else
                {// Error
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return result;
    }

    public static async Task<NetResult> StreamToSendStream(Stream stream, SendStream sendStream, CancellationToken cancellationToken = default)
    {
        var result = NetResult.Success;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long totalSent = 0;
        try
        {
            int length;
            while ((length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                result = await sendStream.Send(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                if (result != NetResult.Success)
                {
                    return result;
                }

                totalSent += length;
            }

            await sendStream.Complete(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await sendStream.Cancel(cancellationToken);
            result = NetResult.Canceled;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return result;
    }

    public static async Task<NetResultValue<TReceive>> StreamToSendStream<TReceive>(Stream stream, SendStreamAndReceive<TReceive> sendStream, CancellationToken cancellationToken = default)
    {
        var result = NetResult.Success;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long totalSent = 0;
        try
        {
            int length;
            while ((length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                result = await sendStream.Send(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                if (result != NetResult.Success)
                {
                    return new(result);
                }

                totalSent += length;
            }

            var r = await sendStream.CompleteSendAndReceive(cancellationToken).ConfigureAwait(false);
            return r;
        }
        catch
        {
            await sendStream.Cancel(cancellationToken);
            result = NetResult.Canceled;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return new(result);
    }
}
