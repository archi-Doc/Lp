// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;

namespace LP;

internal class StorageKeyVault : IStorageKey
{
    private const string Prefix = "S3Bucket/";

    public StorageKeyVault(Vault vault)
    {
        this.vault = vault;
    }

    bool IStorageKey.AddKey(string bucket, AccessKeyPair accessKeyPair)
    {
        var decrypted = this.utf8.GetBytes(accessKeyPair.ToString());
        return this.vault.TryAdd(Prefix + bucket, decrypted);
    }

    bool IStorageKey.TryGetKey(string bucket, out AccessKeyPair accessKeyPair)
    {
        accessKeyPair = default;
        if (!this.vault.TryGet(Prefix + bucket, out var decrypted))
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

    private Vault vault;
    private Encoding utf8 = new UTF8Encoding(true, false);
}
