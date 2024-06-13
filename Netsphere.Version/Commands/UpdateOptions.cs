// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere.Version;

public partial record UpdateOptions
{
    [SimpleOption("address", Description = "Target address")]
    public string Address { get; init; } = string.Empty;

    [SimpleOption(NetConstants.RemotePrivateKeyName, Description = "Private key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption("version_identifier", Description = "Version identifier", GetEnvironmentVariable = true)]
    public int VersionIdentifier { get; set; }

    [SimpleOption("kind", Description = "Version kind (development, release)")]
    public VersionInfo.Kind VersionKind { get; init; } = VersionInfo.Kind.Development;
}
