// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace RemoteDataServer;

public class RemoteData
{
    public RemoteData(UnitOptions unitOptions)
    {
        this.baseDirectory = string.IsNullOrEmpty(unitOptions.DataDirectory) ?
            unitOptions.RootDirectory : unitOptions.DataDirectory;
    }

    #region FieldAndProperty

    public bool Initialized { get; private set; }

    public string Directory { get; private set; } = string.Empty;

    public SignaturePublicKey RemotePublicKey { get; set; }

    private readonly string baseDirectory;

    #endregion

    public void Initialize(string directory)
    {
        this.Directory = UnitHelper.CombineDirectory(this.baseDirectory, directory);
        System.IO.Directory.CreateDirectory(this.Directory);

        this.Initialized = true;
    }

    public async NetTask<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        var transmissionContext = TransmissionContext.Current;
        if (!transmissionContext.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }

    public async NetTask<ReceiveStream?> Get(string identifier)
    {
        var transmissionContext = TransmissionContext.Current;
        this.ThrowIfNotInitialized();
        var path = this.IdentifierToPath(identifier);
        if (path is null)
        {
            transmissionContext.Result = NetResult.NotFound;
            return default;
        }

        try
        {
            using (var fs = File.OpenRead(path))
            {
                (_, var sendStream) = transmissionContext.GetSendStream(100);
                if (sendStream is null)
                {
                    transmissionContext.Result = NetResult.NotFound;
                    return default;
                }

                var b = new byte[1024];
                int length;
                while ((length = await fs.ReadAsync(b)) > 0)
                {
                    await sendStream.Send(b.AsMemory(0, length));
                }

                await sendStream.CompleteSend();
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

        var stream = TransmissionContext.Current.GetReceiveStream();
        if (stream is null)
        {
            return default;
        }

        await stream.Receive(default);

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
