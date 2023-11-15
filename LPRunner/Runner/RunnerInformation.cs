// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using LP;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;

namespace LPRunner;

[TinyhandObject(ImplicitKeyAsName = true, UseServiceProvider = true)]
public partial record RunnerInformation
{
    public const string Path = "RunnerInformation.tinyhand";

    public RunnerInformation(ILogger<RunnerInformation> logger)
    {
        this.logger = logger;
    }

    public RunnerInformation Restore()
    {
        if (!string.IsNullOrEmpty(this.NodeKeyBase64) &&
            NodePrivateKey.TryParse(this.NodeKeyBase64, out var privateKey))
        {
            this.NodeKey = privateKey;
        }
        else
        {
            this.NodeKey = NodePrivateKey.Create();
            this.NodeKeyBase64 = this.NodeKey.UnsafeToString();
        }

        if (!string.IsNullOrEmpty(this.RemotePublicKeyBase64) &&
            SignaturePublicKey.TryParse(this.RemotePublicKeyBase64, out var publicKey))
        {
            this.RemotePublicKey = publicKey;
        }
        else
        {
            this.RemotePublicKey = SignaturePrivateKey.Create().ToPublicKey();
            this.RemotePublicKeyBase64 = this.RemotePublicKey.ToString();
        }

        this.Image = string.IsNullOrEmpty(this.Image) ? "archidoc422/lpconsole" : this.Image;
        this.Tag = string.IsNullOrEmpty(this.Tag) ? "latest" : this.Tag;
        this.RunnerPort = this.RunnerPort == 0 ? 49999 : this.RunnerPort;
        this.HostDirectory = string.IsNullOrEmpty(this.HostDirectory) ? "lp" : this.HostDirectory;
        this.HostPort = this.HostPort == 0 ? 49152 : this.HostPort;
        this.DestinationDirectory = string.IsNullOrEmpty(this.DestinationDirectory) ? "/lp" : this.DestinationDirectory;
        this.DestinationPort = this.DestinationPort == 0 ? 49152 : this.DestinationPort;
        this.RemotePublicKeyBase64 = string.IsNullOrEmpty(this.RemotePublicKeyBase64) ? SignaturePrivateKey.Create().ToPublicKey().ToString() : this.RemotePublicKeyBase64;
        this.NetsphereOptions = string.IsNullOrEmpty(this.NetsphereOptions) ? "-test false -alternative false -logger false" : this.NetsphereOptions;

        return this;
    }

    public string NodeName { get; set; } = string.Empty;

    public string NodeKeyBase64 { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public int RunnerPort { get; set; }

    public string HostDirectory { get; set; } = string.Empty;

    public int HostPort { get; set; }

    public string DestinationDirectory { get; set; } = string.Empty;

    public int DestinationPort { get; set; }

    public string RemotePublicKeyBase64 { get; set; } = string.Empty;

    public string NetsphereOptions { get; set; } = string.Empty;

    public string AdditionalArgs { get; set; } = string.Empty;

    public async Task<bool> Load(string path)
    {
        try
        {
            var utf8 = await File.ReadAllBytesAsync(path);
            var information = this;
            TinyhandSerializer.DeserializeObjectFromUtf8(utf8, ref information);

            // Update RunnerInformation
            this.Restore();
            var update = TinyhandSerializer.SerializeToUtf8(this);
            if (!update.SequenceEqual(utf8))
            {
                await File.WriteAllBytesAsync(path, update);
            }

            return true;
        }
        catch
        {
        }

        this.Restore();
        await File.WriteAllBytesAsync(path, TinyhandSerializer.SerializeToUtf8(this));

        this.logger.TryGet(LogLevel.Error)?.Log($"'{path}' could not be found and was created.");
        this.logger.TryGet(LogLevel.Error)?.Log($"Modify '{RunnerInformation.Path}', and restart LPRunner.");

        return false;
    }

    [IgnoreMember]
    internal NodePrivateKey NodeKey { get; set; } = default!;

    [IgnoreMember]
    internal SignaturePublicKey RemotePublicKey { get; set; } = default!;

    internal NetAddress TryGetDualAddress()
    {
        var text = $"127.0.0.1:{this.DestinationPort}";
        NetAddress.TryParse(text, out var address);
        return address;
    }

    private ILogger logger;
}
