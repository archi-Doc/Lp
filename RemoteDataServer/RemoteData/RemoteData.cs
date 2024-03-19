// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.IO;
using Netsphere.Crypto;

namespace RemoteDataServer;

public class RemoteData
{
    private const int ReadBufferSize = 1024 * 1024 * 4;

    public RemoteData(UnitOptions unitOptions)
    {
        this.baseDirectory = string.IsNullOrEmpty(unitOptions.DataDirectory) ?
            unitOptions.RootDirectory : unitOptions.DataDirectory;
        this.limitAreement = new ConnectionAgreement() with
        {
            MaxStreamLength = 100,
        };
    }

    #region FieldAndProperty

    public bool Initialized { get; private set; }

    public string Directory { get; private set; } = string.Empty;

    public SignaturePublicKey RemotePublicKey { get; set; }

    private readonly ConnectionAgreement limitAreement;
    private readonly string baseDirectory;

    #endregion

    public void Initialize(string directory)
    {
        this.Directory = PathHelper.CombineDirectory(this.baseDirectory, directory);
        System.IO.Directory.CreateDirectory(this.Directory);

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
            try
            {
                int length;
                while ((length = await fileStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await sendStream.Send(buffer.AsMemory(0, length)).ConfigureAwait(false);
                }

                await sendStream.CompleteSend().ConfigureAwait(false);
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

        var result = NetResult.Success;
        try
        {
            using var fileStream = File.Create(path);
            using var receiveStream = TransmissionContext.Current.GetReceiveStream();

            var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            try
            {
                while (true)
                {
                    (result, var written) = await receiveStream.Receive(buffer).ConfigureAwait(false);
                    if (written == 0)
                    {// Completed or error.
                        break;
                    }
                    else
                    {// written > 0
                        await fileStream.WriteAsync(buffer.AsMemory(0, written)).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch
        {
            transmissionContext.Result = NetResult.InvalidOperation;
            return default;
        }

        if (result != NetResult.Completed)
        {
            Arc.Unit.PathHelper.TryDeleteFile(path);
        }

        return default;
    }

    private string? IdentifierToPath(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }
        else if (Path.IsPathRooted(identifier))
        {
            return null;
        }

        return Path.Combine(this.Directory, identifier);
    }

    private void ThrowIfNotInitialized()
    {
        if (!this.Initialized)
        {
            throw new InvalidOperationException();
        }
    }
}
