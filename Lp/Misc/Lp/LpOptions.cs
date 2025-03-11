// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LpOptions
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("LoadOptions", Description = "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("VaultPass", Description = "Passphrase for vault", ReadFromEnvironment = true)]
    public string? VaultPass { get; set; } = null;

    [SimpleOption("Port", Description = "Port number associated with the address", ReadFromEnvironment = true)]
    public int Port { get; set; }

    [SimpleOption("Test", Description = "Enable test features")]
    public bool TestFeatures { get; set; } = false;

    [SimpleOption("DataDirectory", Description = "Data directory", ReadFromEnvironment = true)]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("VaultPath", Description = "Vault path")]
    public string VaultPath { get; init; } = string.Empty;

    // [SimpleOption("remotekey", Description = "Base64 representation of remote public key")]
    // public string RemotePublicKeyBase64 { get; init; } = string.Empty;

    [SimpleOption("NodeName", Description = "Node name", ReadFromEnvironment = true)]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("NodeList", Description = "Node list", ReadFromEnvironment = true)]
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

    [SimpleOption(NetConstants.NodeSecretKeyName, Description = "Node secret key", ReadFromEnvironment = true)]
    public string NodeSecretKey { get; set; } = string.Empty;

    [SimpleOption(NetConstants.RemotePublicKeyName, Description = "Remote public key", ReadFromEnvironment = true)]
    public string RemotePublicKey { get; set; } = string.Empty;

    [SimpleOption("CertificateRelayPublickey", Description = "Public key for CertificateRelayControl", ReadFromEnvironment = true)]
    public string CertificateRelayPublicKey { get; set; } = string.Empty;

    [SimpleOption("RelayPeerPrivault", Description = "Private key or vault name for Relay peer", ReadFromEnvironment = true)]
    public string RelayPeerPrivault { get; set; } = "RelayPeer";

    [SimpleOption("ContentPeerPrivault", Description = "Private key or vault name for Content peer", ReadFromEnvironment = true)]
    public string ContentPeerPrivault { get; set; } = "ContentPeer";

    [SimpleOption("MergerPrivault", Description = "Private key or vault name for Merger", ReadFromEnvironment = true)]
    public string MergerPrivault { get; set; } = "Merger";

    [SimpleOption("RelayMergerPrivault", Description = "Private key or vault name for Relay merger", ReadFromEnvironment = true)]
    public string RelayMergerPrivault { get; set; } = "RelayMerger";

    [SimpleOption("LinkerPrivault", Description = "Private key or vault name for Linker", ReadFromEnvironment = true)]
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
