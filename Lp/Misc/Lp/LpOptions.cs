// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LpOptions
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("LoadOptions", Description = "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("VaultPass", Description = "Passphrase for vault", GetEnvironmentVariable = true)]
    public string? VaultPass { get; set; } = null;

    [SimpleOption("Port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("Test", Description = "Enable test features")]
    public bool TestFeatures { get; set; } = false;

    [SimpleOption("RootDir", Description = "Root directory")]
    public string RootDirectory { get; init; } = string.Empty;

    [SimpleOption("DataDir", Description = "Data directory")]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("VaultPath", Description = "Vault path")]
    public string VaultPath { get; init; } = string.Empty;

    // [SimpleOption("remotekey", Description = "Base64 representation of remote public key")]
    // public string RemotePublicKeyBase64 { get; init; } = string.Empty;

    [SimpleOption("NodeName", Description = "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("NodeList", Description = "Node list", GetEnvironmentVariable = true)]
    public string NodeList { get; init; } = string.Empty;

    [SimpleOption("Lifespan", Description = "Time in seconds until the application automatically shuts down.")]
    public long Lifespan { get; init; }

    [SimpleOption("ConfirmExit", Description = "Confirms application exit")]
    public bool ConfirmExit { get; init; } = false;

    [SimpleOption("ColorConsole", Description = "Enable color console")]
    public bool ColorConsole { get; init; } = true;

    [SimpleOption("Ping", Description = "Enable ping function")]
    public bool EnablePing { get; set; } = true;

    [SimpleOption("Server", Description = "Enable server function")]
    public bool EnableServer { get; set; } = false;

    [SimpleOption("Alternative", Description = "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;

    [SimpleOption(NetConstants.NodeSecretKeyName, Description = "Node secret key", GetEnvironmentVariable = true)]
    public string NodeSecretKey { get; set; } = string.Empty;

    [SimpleOption(NetConstants.RemotePublicKeyName, Description = "Remote public key", GetEnvironmentVariable = true)]
    public string RemotePublicKey { get; set; } = string.Empty;

    [SimpleOption("CertificateRelayPublickey", Description = "Public key for CertificateRelayControl", GetEnvironmentVariable = true)]
    public string CertificateRelayPublicKey { get; set; } = string.Empty;

    [SimpleOption("RelayPeerPrivault", Description = "Private key or vault name for Relay peer", GetEnvironmentVariable = true)]
    public string RelayPeerPrivault { get; set; } = "RelayPeer";

    [SimpleOption("ContentPeerPrivault", Description = "Private key or vault name for Content peer", GetEnvironmentVariable = true)]
    public string ContentPeerPrivault { get; set; } = "ContentPeer";

    [SimpleOption("MergerPrivault", Description = "Private key or vault name for Merger", GetEnvironmentVariable = true)]
    public string MergerPrivault { get; set; } = "Merger";

    [SimpleOption("RelayMergerPrivault", Description = "Private key or vault name for Relay merger", GetEnvironmentVariable = true)]
    public string RelayMergerPrivault { get; set; } = "RelayMerger";

    [SimpleOption("LinkerPrivault", Description = "Private key or vault name for Linker", GetEnvironmentVariable = true)]
    public string LinkerPrivault { get; set; } = "Linker";

    public NetOptions ToNetOptions()
    {
        return new NetOptions() with
        {
            Port = this.Port,
            NodeSecretKey = this.NodeSecretKey,
            NodeList = this.NodeList,
            EnablePing = this.EnablePing,
            EnableServer = this.EnableServer,
            EnableAlternative = this.EnableAlternative,
        };
    }
}
