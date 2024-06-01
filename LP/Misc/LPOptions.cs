// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPOptions
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("loadoptions", Description = "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("vault_pass", Description = "Passphrase for vault")]
    public string? VaultPass { get; set; } = null;

    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("creditmerger", Description = "Enable credit merger feature")]
    public bool CreditMerger { get; init; } = true;

    [SimpleOption("relaymerger", Description = "Enable relay merger feature")]
    public bool RelayMerger { get; init; } = true;

    [SimpleOption("volatilepeer", Description = "Enable volatile peer feature")]
    public bool VolatilePeer { get; init; } = true;

    [SimpleOption("nonvolatilepeer", Description = "Enable non-volatile peer feature")]
    public bool NonVolatilePeer { get; init; } = true;

    [SimpleOption("test", Description = "Enable test features")]
    public bool TestFeatures { get; set; } = false;

    [SimpleOption("rootdir", Description = "Root directory")]
    public string RootDirectory { get; init; } = string.Empty;

    [SimpleOption("datadir", Description = "Data directory")]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("vault", Description = "Vault path")]
    public string VaultPath { get; init; } = string.Empty;

    // [SimpleOption("remotekey", Description = "Base64 representation of remote public key")]
    // public string RemotePublicKeyBase64 { get; init; } = string.Empty;

    [SimpleOption("name", Description = "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("confirmexit", Description = "Confirms application exit")]
    public bool ConfirmExit { get; init; } = false;

    [SimpleOption("colorconsole", Description = "Enable color console")]
    public bool ColorConsole { get; init; } = true;

    [SimpleOption("lifespan", Description = "Time in seconds until the application automatically shuts down.")]
    public long Lifespan { get; init; }

    [SimpleOption("nodeprivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; set; } = string.Empty;

    [SimpleOption("relaypublickey", Description = "Relay public key (CertificateRelayControl)")]
    public string RelayPublicKey { get; set; } = string.Empty;

    [SimpleOption("ping", Description = "Enable ping function")]
    public bool EnablePing { get; set; } = true;

    [SimpleOption("server", Description = "Enable server function")]
    public bool EnableServer { get; set; } = false;

    [SimpleOption("alternative", Description = "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;

    [SimpleOption("relay_certificate_priauth", Description = "Private key or authority name for CertificateRelayControl")]
    public string RelayCertificatePriauth { get; set; } = string.Empty;

    [SimpleOption("relay_peer_priauth", Description = "Private key or authority name for Relay peer")]
    public string RelayPeerPriauth { get; set; } = "relay_peer";

    public NetOptions ToNetOptions()
    {
        return new NetOptions() with
        {
            Port = this.Port,
            NodePrivateKey = this.NodePrivateKey,
            EnablePing = this.EnablePing,
            EnableServer = this.EnableServer,
            EnableAlternative = this.EnableAlternative,
        };
    }

    public void LoadEnvironmentVariables()
    {
        if (string.IsNullOrEmpty(this.RelayPublicKey))
        {
            this.RelayPublicKey = Environment.GetEnvironmentVariable("relaypublickey") ?? string.Empty;
        }
    }

    public bool RequiredMergerPrivateKey
        => this.CreditMerger || this.RelayMerger;
}
