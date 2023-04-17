// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Check;

[TinyhandObject]
public readonly partial struct DataAndConfigurationIdentifier : IEquatable<DataAndConfigurationIdentifier>
{
    public DataAndConfigurationIdentifier(string typeFullName, PathConfiguration configuration)
    {
        this.TypeFullName = typeFullName;
        this.PathConfiguration = configuration;
    }

    [Key(0)]
    public readonly string TypeFullName;

    [Key(1)]
    public readonly PathConfiguration PathConfiguration;

    public override int GetHashCode()
        => HashCode.Combine(this.TypeFullName, this.PathConfiguration);

    public bool Equals(DataAndConfigurationIdentifier other)
        => this.TypeFullName.Equals(other.TypeFullName) &&
        this.PathConfiguration.Equals(other.PathConfiguration);
}
