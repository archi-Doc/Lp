// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;

namespace Lp;

internal class StorageKeyVault : IStorageKey
{
    private const string Prefix = "S3Bucket/";

    public StorageKeyVault()
    {
    }

    bool IStorageKey.AddKey(string bucket, AccessKeyPair accessKeyPair)
    {
        if (this.Vault is not { } vault)
        {
            return false;
        }

        var decrypted = this.utf8.GetBytes(accessKeyPair.ToString());
        return vault.TryAdd(Prefix + bucket, decrypted);
    }

    bool IStorageKey.TryGetKey(string bucket, out AccessKeyPair accessKeyPair)
    {
        accessKeyPair = default;

        if (this.Vault is not { } vault)
        {
            return false;
        }

        if (!vault.TryGet(Prefix + bucket, out var decrypted))
        {
            return false;
        }

        try
        {
            var st = this.utf8.GetString(decrypted);
            if (AccessKeyPair.TryParse(st, out accessKeyPair))
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public Vault? Vault { get; set; }

    private Encoding utf8 = new UTF8Encoding(true, false);
}
