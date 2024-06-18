// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LpOptions
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("loadoptions", Description = "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("vault_pass", Description = "Passphrase for vault", GetEnvironmentVariable = true)]
    public string? VaultPass { get; set; } = null;

    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("test", Description = "Enable test features")]
    public bool TestFeatures { get; set; } = false;

    [SimpleOption("rootdir", Description = "Root directory")]
    public string RootDirectory { get; init; } = string.Empty;

    [SimpleOption("datadir", Description = "Data directory")]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("vault_path", Description = "Vault path")]
    public string VaultPath { get; init; } = string.Empty;

    // [SimpleOption("remotekey", Description = "Base64 representation of remote public key")]
    // public string RemotePublicKeyBase64 { get; init; } = string.Empty;

    [SimpleOption("name", Description = "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("node_list", Description = "Node list", GetEnvironmentVariable = true)]
    public string NodeList { get; init; } = string.Empty;

    [SimpleOption("lifespan", Description = "Time in seconds until the application automatically shuts down.")]
    public long Lifespan { get; init; }

    [SimpleOption("confirmexit", Description = "Confirms application exit")]
    public bool ConfirmExit { get; init; } = false;

    [SimpleOption("colorconsole", Description = "Enable color console")]
    public bool ColorConsole { get; init; } = true;

    [SimpleOption("ping", Description = "Enable ping function")]
    public bool EnablePing { get; set; } = true;

    [SimpleOption("server", Description = "Enable server function")]
    public bool EnableServer { get; set; } = false;

    [SimpleOption("alternative", Description = "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;

    [SimpleOption(NetConstants.NodePrivateKeyName, Description = "Node private key", GetEnvironmentVariable = true)]
    public string NodePrivateKey { get; set; } = string.Empty;

    [SimpleOption("certificate_relay_publickey", Description = "Public key for CertificateRelayControl", GetEnvironmentVariable = true)]
    public string CertificateRelayPublicKey { get; set; } = string.Empty;

    [SimpleOption("relay_peer_privault", Description = "Private key or vault name for Relay peer", GetEnvironmentVariable = true)]
    public string RelayPeerPrivault { get; set; } = "relay_peer";

    [SimpleOption("content_peer_privault", Description = "Private key or vault name for Content peer", GetEnvironmentVariable = true)]
    public string ContentPeerPrivault { get; set; } = "content_peer";

    [SimpleOption("credit_merger_privault", Description = "Private key or vault name for Credit merger", GetEnvironmentVariable = true)]
    public string CreditMergerPrivault { get; set; } = "credit_merger";

    [SimpleOption("relay_merger_privault", Description = "Private key or vault name for Relay merger", GetEnvironmentVariable = true)]
    public string RelayMergerPrivault { get; set; } = "relay_merger";

    public NetOptions ToNetOptions()
    {
        return new NetOptions() with
        {
            Port = this.Port,
            NodePrivateKey = this.NodePrivateKey,
            NodeList = this.NodeList,
            EnablePing = this.EnablePing,
            EnableServer = this.EnableServer,
            EnableAlternative = this.EnableAlternative,
        };
    }
}
