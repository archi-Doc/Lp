// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.IO;
using Netsphere;
using Netsphere.Crypto;

namespace RemoteDataServer;

public class RemoteDataControl
{
    private const int ReadBufferSize = 1024 * 1024 * 4;

    public RemoteDataControl(UnitOptions unitOptions, ILogger<RemoteDataControl> logger)
    {
        this.logger = logger;
        this.baseDirectory = string.IsNullOrEmpty(unitOptions.DataDirectory) ?
            unitOptions.RootDirectory : unitOptions.DataDirectory;
        this.limitAreement = new ConnectionAgreement() with
        {
            MaxStreamLength = 100_000_000,
        };
    }

    #region FieldAndProperty

    public bool Initialized { get; private set; }

    public string DataDirectory { get; private set; } = string.Empty;

    public SignaturePublicKey RemotePublicKey { get; set; }

    private readonly ILogger logger;
    private readonly ConnectionAgreement limitAreement;
    private readonly string baseDirectory;

    #endregion

    public void Initialize(string directory)
    {
        this.DataDirectory = PathHelper.CombineDirectory(this.baseDirectory, directory);
        Directory.CreateDirectory(this.DataDirectory);

        this.Initialized = true;
    }

    public async NetTask<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        this.ThrowIfNotInitialized();

        var transmissionContext = TransmissionContext.Current;
        if (!transmissionContext.ServerConnection.ValidateAndVerifyWithSalt(token))
        {// Invalid token
            return NetResult.NotAuthorized;
        }
        else if (!token.PublicKey.Equals(this.RemotePublicKey))
        {// Invalid public key
            return NetResult.NotAuthorized;
        }

        if (!token.Target.IsInclusive(this.limitAreement))
        {
            return NetResult.Refused;
        }

        return NetResult.Success;
    }

    public async NetTask<ReceiveStream?> Get(string identifier)
    {
        this.ThrowIfNotInitialized();

        var transmissionContext = TransmissionContext.Current;
        var path = this.IdentifierToPath(identifier);
        if (path is null)
        {
            transmissionContext.Result = NetResult.NotFound;
            return default;
        }

        try
        {
            using var fileStream = File.OpenRead(path);
            (_, var sendStream) = transmissionContext.GetSendStream(fileStream.Length);
            if (sendStream is null)
            {
                transmissionContext.Result = NetResult.NotFound;
                return default;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            long totalSent = 0;
            try
            {
                int length;
                while ((length = await fileStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await sendStream.Send(buffer.AsMemory(0, length)).ConfigureAwait(false);
                    totalSent += length;
                }

                await sendStream.Complete().ConfigureAwait(false);
                this.logger.TryGet(LogLevel.Information)?.Log($"Get: {identifier} {totalSent} bytes");
            }
            catch
            {
                await sendStream.Cancel();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch
        {
            transmissionContext.Result = NetResult.NotFound;
            return default;
        }

        return default;
    }

    public async NetTask<SendStreamAndReceive<NetResult>?> Put(string identifier, long maxLength)
    {
        this.ThrowIfNotInitialized();

        var transmissionContext = TransmissionContext.Current;
        var path = this.IdentifierToPath(identifier);
        if (path is null)
        {
            transmissionContext.Result = NetResult.InvalidOperation;
            return default;
        }

        var result = NetResult.UnknownError;
        long totalWritten = 0;
        try
        {
            using var fileStream = File.Create(path);
            var receiveStream = transmissionContext.GetReceiveStream<NetResult>();

            var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            try
            {
                while (true)
                {
                    (result, var written) = await receiveStream.Receive(buffer).ConfigureAwait(false);
                    if (written == 0)
                    {// Completed or error.
                        // transmissionContext.SendAndForget(result);
                        break;
                    }
                    else
                    {// written > 0
                        await fileStream.WriteAsync(buffer.AsMemory(0, written)).ConfigureAwait(false);
                        totalWritten += written;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);

                if (result == NetResult.Completed)
                {// Complete
                 // transmissionContext.Result = NetResult.Success;
                    result = NetResult.Success;
                    this.logger.TryGet(LogLevel.Information)?.Log($"Put: {identifier} {totalWritten} bytes");
                    // this.logger.TryGet(LogLevel.Information)?.Log($"{maxLength}, {receiveStream.MaxStreamLength}, {receiveStream.ReceivedLength}");
                }
                else
                {
                    PathHelper.TryDeleteFile(path);
                    // transmissionContext.Result = result;
                }

                receiveStream.SendAndDispose(result);
            }
        }
        catch
        {
            transmissionContext.Result = NetResult.InvalidOperation;
            return default;
        }

        return default;
    }

    private string? IdentifierToPath(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }
        else if (identifier.Contains("../") ||
            identifier.Contains("..\\"))
        {
            return null;
        }
        else if (Path.IsPathRooted(identifier))
        {
            return null;
        }

        return Path.Combine(this.DataDirectory, identifier);
    }

    private void ThrowIfNotInitialized()
    {
        if (!this.Initialized)
        {
            throw new InvalidOperationException();
        }
    }
}
