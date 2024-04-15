// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using LP.Data;
using Netsphere.Crypto;
using Netsphere.Misc;

namespace LP;

public enum LPMode
{
    Merger,
    Relay,
    Automaton,
    Replicator,
    Karate,
}

public class LPBase
{
    public const string DataDirectoryName = "Local";

    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.AddSingleton<LPBase>();
    }

    public LPBase()
    {
        this.Settings = TinyhandSerializer.Reconstruct<LPSettings>();
    }

    public bool IsFirstRun { get; private set; }

    public bool IsConsole { get; private set; }

    public string RootDirectory { get; private set; } = default!;

    public string DataDirectory { get; private set; } = default!;

    public LPMode Mode { get; internal set; }

    public bool TestFeatures { get; private set; }

    public string NodeName { get; private set; } = default!;

    public SignaturePublicKey RemotePublicKey { get; private set; }

    public LPOptions Options { get; private set; } = default!;

    public LPSettings Settings { get; set; }

    // public string GetRootPath(string path, string defaultFilename) => this.GetPath(this.RootDirectory, path, defaultFilename);

    public string CombineDataPath(string path, string defaultFilename) => this.CombinePath(this.DataDirectory, path, defaultFilename);

    public string CombineDataPathAndPrepareDirectory(string path, string defaultFilename)
    {
        var file = this.CombineDataPath(path, defaultFilename);

        try
        {
            if (Path.GetDirectoryName(file) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch
        {
        }

        return file;
    }

    public async Task<T?> TryLoadUtf8Async<T>(string filename)
    {
        var path = Path.Combine(this.DataDirectory, filename);
        try
        {
            if (File.Exists(path))
            {
                var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
                return TinyhandSerializer.DeserializeFromUtf8<T>(bytes);
            }
        }
        catch
        {
        }

        return default;
    }

    public async Task<bool> SaveUtf8Async<T>(string filename, T obj)
    {
        var path = Path.Combine(this.DataDirectory, filename);
        try
        {
            if (Path.GetDirectoryName(path) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }

            var bytes = TinyhandSerializer.SerializeToUtf8(obj);
            await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
            return true;
        }
        catch
        {
        }

        return false;
    }

    public void Initialize(LPOptions options, bool isConsole, string defaultMode)
    {
        this.Options = options;
        this.IsConsole = isConsole;

        // Root directory
        if (Path.IsPathRooted(this.Options.RootDirectory))
        {// File.GetAttributes(this.Options.RootDirectory).HasFlag(FileAttributes.Directory)
            this.RootDirectory = this.Options.RootDirectory;
        }
        else
        {
            this.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), this.Options.RootDirectory);
        }

        Directory.CreateDirectory(this.RootDirectory);
        this.DataDirectory = Path.Combine(this.RootDirectory, DataDirectoryName);
        this.IsFirstRun = !Directory.Exists(this.DataDirectory);

        // Mode
        LPMode mode;
        if (!Enum.TryParse<LPMode>(this.Options.Mode, true, out mode))
        {
            if (!Enum.TryParse<LPMode>(defaultMode, true, out mode))
            {
                mode = LPMode.Merger;
            }
        }

        this.Mode = mode;
        this.TestFeatures = options.TestFeatures;

        this.NodeName = this.Options.NodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }

        // Remote public key
        SignaturePublicKey.TryParse(options.RemotePublicKeyBase64, out var remoteKey);
        this.RemotePublicKey = remoteKey;
    }

    public void LogInformation(ILogWriter logger)
    {
        logger.Log($"Root directory: {this.RootDirectory}");
        logger.Log($"Data directory: {this.DataDirectory}");
        logger.Log($"Node: {this.NodeName}, Mode: {this.Mode.ToString()}, Test: {this.TestFeatures}");
        // logger.Log(this.Options.ToString());
    }

    private string CombinePath(string directory, string path, string defaultFilename)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        else
        {
            return Path.Combine(directory, string.IsNullOrEmpty(path) ? defaultFilename : path);
        }
    }
}
