// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using LP;
using Tinyhand;

namespace LPRunner;

[TinyhandObject(ImplicitKeyAsName = true, UseServiceProvider = true)]
public partial record RunnerInformation
{
    public const string Path = "RunnerInformation.tinyhand";

    public RunnerInformation(LPBase lpBase, ILogger<RunnerInformation> logger)
    {
        this.lpBase = lpBase;
        this.logger = logger;
    }

    public RunnerInformation SetDefault()
    {
        this.Image = string.IsNullOrEmpty(this.Image) ? "archidoc422/lpconsole" : this.Image;
        this.Tag = string.IsNullOrEmpty(this.Tag) ? "latest" : this.Tag;
        this.RunnerPort = this.RunnerPort == 0 ? 49999 : this.RunnerPort;
        this.HostDirectory = string.IsNullOrEmpty(this.HostDirectory) ? "lp" : this.HostDirectory;
        this.HostPort = this.HostPort == 0 ? 49152 : this.HostPort;
        this.DestinationDirectory = string.IsNullOrEmpty(this.DestinationDirectory) ? "/lp" : this.DestinationDirectory;
        this.DestinationPort = this.DestinationPort == 0 ? 49152 : this.DestinationPort;
        this.PublicKeyHex = string.IsNullOrEmpty(this.PublicKeyHex) ? PrivateKey.Create().ToPublicKey().ToString() : this.PublicKeyHex;
        return this;
    }

    public string Image { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public int RunnerPort { get; set; }

    public string HostDirectory { get; set; } = string.Empty;

    public int HostPort { get; set; }

    public string DestinationDirectory { get; set; } = string.Empty;

    public int DestinationPort { get; set; }

    public string PublicKeyHex { get; set; } = string.Empty;

    public async Task<bool> Load(string path)
    {
        try
        {
            var utf8 = await File.ReadAllBytesAsync(path);
            var information = TinyhandSerializer.DeserializeWithFromUtf8<RunnerInformation>(this, utf8);
            if (information != null)
            {// Success
             // Update RunnerInformation
                this.SetDefault();
                var update = TinyhandSerializer.SerializeToUtf8(this);
                if (!update.SequenceEqual(utf8))
                {
                    await File.WriteAllBytesAsync(path, update);
                }

                return true;
            }
        }
        catch
        {
        }

        this.SetDefault();
        await File.WriteAllBytesAsync(path, TinyhandSerializer.SerializeToUtf8(this));

        this.logger.TryGet(LogLevel.Error)?.Log($"'{path}' could not be found and was created.");
        this.logger.TryGet(LogLevel.Error)?.Log($"Modify '{RunnerInformation.Path}', and restart LPRunner.");

        return false;
    }

    [IgnoreMember]
    internal PublicKey PublicKey => new PublicKey(this.PublicKeyHex);

    private LPBase lpBase;
    private ILogger logger;
}
