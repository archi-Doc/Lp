// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public readonly struct AccessKeyPair : IEquatable<AccessKeyPair>
{
    public AccessKeyPair()
    {
        this.AccessKeyId = string.Empty;
        this.SecretAccessKey = string.Empty;
    }

    public AccessKeyPair(string accessKeyId, string secretAccessKey)
    {
        this.AccessKeyId = accessKeyId;
        this.SecretAccessKey = secretAccessKey;
    }

    public readonly string AccessKeyId;

    public readonly string SecretAccessKey;

    public override int GetHashCode()
        => HashCode.Combine(this.AccessKeyId, this.SecretAccessKey);

    public bool Equals(AccessKeyPair other)
        => this.AccessKeyId == other.AccessKeyId &&
        this.SecretAccessKey == other.SecretAccessKey;
}
