// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Netsphere.Crypto;
using Netsphere.Relay;
using Tinyhand;

namespace Netsphere.Version;

[TinyhandObject]
public partial record VersionData
{
    private const string Filename = "Version.tinyhand";

    public VersionData()
    {
    }

    public static VersionData Load()
    {
        try
        {
            var bin = File.ReadAllBytes(Filename);
            return TinyhandSerializer.DeserializeObjectFromUtf8<VersionData>(bin) ?? new();
        }
        catch
        {
            return new();
        }
    }

    [Key(0)]
    public CertificateToken<VersionInfo>? Development { get; private set; }

    [Key(1)]
    public CertificateToken<VersionInfo>? Release { get; private set; }

    private GetVersionResponse developmentResponse = new();
    private GetVersionResponse releaseResponse = new();

    public GetVersionResponse? GetVersionResponse(VersionInfo.Kind versionKind)
    {
        if (versionKind == VersionInfo.Kind.Development)
        {
            return this.developmentResponse;
        }
        else if (versionKind == VersionInfo.Kind.Release)
        {
            return this.releaseResponse;
        }
        else
        {
            return default;
        }
    }

    public void Log(ILogger logger)
    {
        logger.TryGet()?.Log($"Development: {this.Development?.Target.ToString()}, Release: {this.Release?.Target.ToString()}");
    }
}
