﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;
using Tinyhand.IO;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPOptions : ILogInformation
{
    public const string DefaultOptionsName = "Options.tinyhand";

    [SimpleOption("loadoptions", description: "Options path")]
    public string OptionsPath { get; init; } = string.Empty;

    [SimpleOption("development", description: "Development")]
    public bool Development { get; init; } = false;

    [SimpleOption("mode", description: "LP mode (relay, merger, user)")]
    public string Mode { get; init; } = string.Empty;

    [SimpleOption("rootdir", description: "Root directory")]
    public string RootDirectory { get; init; } = string.Empty;

    [SimpleOption("datadir", description: "Data directory")]
    public string DataDirectory { get; init; } = string.Empty;

    [SimpleOption("vault", description: "Vault path")]
    public string Vault { get; init; } = string.Empty;

    [SimpleOption("name", description: "Node name")]
    public string NodeName { get; init; } = string.Empty;

    [SimpleOption("ns", description: "Netsphere option")]
    public NetsphereOptions NetsphereOptions { get; init; } = default!;

    [SimpleOption("zen", description: "ZenItz option")]
    public ZenItzOptions ZenItzOptions { get; init; } = default!;

    [SimpleOption("confirmexit", description: "Confirms application exit")]
    public bool ConfirmExit { get; init; } = false;

    public void LogInformation(ILog log)
    {
        this.NetsphereOptions.LogInformation(log);
        this.ZenItzOptions.LogInformation(log);
    }

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
