// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPOptions
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("loadoptions", Description = "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("pass", Description = "Passphrase for vault")]
    public string? Pass { get; set; } = null;

    [SimpleOption("development", Description = "Development")]
    public bool Development { get; init; } = false;

    [SimpleOption("mode", Description = "LP mode (merger, relay, automaton, replicator, karate)")]
    public string Mode { get; init; } = string.Empty;

    [SimpleOption("test", Description = "Enable test features")]
    public bool TestFeatures { get; set; } = false;

    [SimpleOption("rootdir", Description = "Root directory")]
    public string RootDirectory { get; init; } = string.Empty;

    [SimpleOption("datadir", Description = "Data directory")]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("vault", Description = "Vault path")]
    public string VaultPath { get; init; } = string.Empty;

    [SimpleOption("remotekey", Description = "Base64 representation of remote public key")]
    public string RemotePublicKeyBase64 { get; init; } = string.Empty;

    [SimpleOption("name", Description = "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("ns", Description = "Netsphere option")]
    public NetsphereOptions NetsphereOptions { get; init; } = default!;

    [SimpleOption("confirmexit", Description = "Confirms application exit")]
    public bool ConfirmExit { get; init; } = false;

    [SimpleOption("colorconsole", Description = "Enable color console")]
    public bool ColorConsole { get; init; } = true;

    [SimpleOption("lifespan", Description = "Time in seconds until the application automatically shuts down.")]
    public long Lifespan { get; init; }

    /*public bool TryLoad()
    {
        if (!string.IsNullOrEmpty(this.Options))
        {
            try
            {
                var utf8 = File.ReadAllBytes(this.Options);
                var writer = default(TinyhandWriter);
                TinyhandTreeConverter.FromUtf8ToBinary(utf8, ref writer);
                var reader = new TinyhandReader(writer.FlushAndGetReadOnlySequence());
                this.Deserialize(ref reader, TinyhandSerializerOptions.Standard);
                return true;
            }
            catch
            {
            }
        }

        return false;
    }*/
}
