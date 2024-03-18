// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace RemoteDataServer;

[NetServiceObject]
public class RemoteDataImpl : IRemoteData
{
    public RemoteDataImpl(UnitOptions unitOptions)
    {
        this.baseDirectory = string.IsNullOrEmpty(unitOptions.DataDirectory) ?
            unitOptions.RootDirectory : unitOptions.DataDirectory;
    }

    #region FieldAndProperty

    public bool Initialized { get; private set; }

    public string Directory { get; private set; } = string.Empty;

    private readonly string baseDirectory;

    #endregion

    public void Initialize(string directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            this.Directory = this.baseDirectory;
        }
        else if (Path.IsPathRooted(directory))
        {// Contains a root.
            this.Directory = directory;
        }
        else
        {
            this.Directory = Path.Combine(this.baseDirectory, directory);
        }

        System.IO.Directory.CreateDirectory(this.Directory);

        this.Initialized = true;
    }

    public async NetTask<ReceiveStream?> Get(string identifier)
    {
        var transmissionContext = TransmissionContext.Current;
        this.ThrowIfNotInitialized();
        var path = this.GetPath(identifier);
        if (path is null)
        {
            transmissionContext.Result = NetResult.NotFound;
            return default;
        }

        (_, var stream) = TransmissionContext.Current.GetSendStream(100);
        if (stream is not null)
        {
            await stream.Send(default);
            await stream.CompleteSend();
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

    private string? GetPath(string identifier)
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

    /*public NetTask<SendStreamAndReceive<NetResult>?> Put2(string identifier, ulong hash, long maxLength)
    {
        throw new NotImplementedException();
    }*/
}
