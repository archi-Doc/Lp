// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

using Lp.Data;
using Netsphere.Crypto;

namespace Lp;

public class LpBase
{
    public const string DataDirectoryName = "Local";

    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.AddSingleton<LpBase>();
    }

    public LpBase()
    {
        this.Settings = TinyhandSerializer.Reconstruct<LpSettings>();
    }

    public bool IsFirstRun { get; private set; }

    public bool IsConsole { get; private set; }

    public string RootDirectory { get; private set; } = default!;

    public string DataDirectory { get; private set; } = default!;

    public string NodeName { get; private set; } = default!;

    // public SignaturePublicKey RemotePublicKey { get; private set; }

    public LpOptions Options { get; private set; } = default!;

    public LpSettings Settings { get; set; }

    private SignaturePublicKey remotePublicKey;

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

    public void Initialize(LpOptions options, bool isConsole, string defaultMode)
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

        this.NodeName = this.Options.NodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }

        if (SignaturePublicKey.TryParse(options.RemotePublicKey, out var publicKey))
        {
            this.remotePublicKey = publicKey;
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<SignaturePublicKey>(NetConstants.RemotePublicKeyName, out publicKey))
        {
            this.remotePublicKey = publicKey;
        }
    }

    public bool TryGetRemotePublicKey(out SignaturePublicKey publicKey)
    {
        if (this.remotePublicKey.IsValid)
        {
            publicKey = this.remotePublicKey;
            return true;
        }
        else
        {
            publicKey = default;
            return false;
        }
    }

    public void LogInformation(ILogWriter logger)
    {
        logger.Log($"Root directory: {this.RootDirectory}");
        logger.Log($"Data directory: {this.DataDirectory}");
        logger.Log($"Node: {this.NodeName}, Test: {this.Options.TestFeatures}");

        if (this.TryGetRemotePublicKey(out var publicKey))
        {
            logger.Log($"Remote public key: {publicKey}");
        }

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
