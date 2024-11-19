﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Lp.Services;

namespace Lp;

internal class StorageKeyVault : IStorageKey
{
    private const string Prefix = "S3Bucket/";

    public StorageKeyVault()
    {
    }

    bool IStorageKey.AddKey(string bucket, AccessKeyPair accessKeyPair)
    {
        if (this.VaultControl is not { } vaultControl)
        {
            return false;
        }

        var decrypted = this.utf8.GetBytes(accessKeyPair.ToString());
        return vaultControl.TryAdd(Prefix + bucket, decrypted);
    }

    bool IStorageKey.TryGetKey(string bucket, out AccessKeyPair accessKeyPair)
    {
        accessKeyPair = default;

        if (this.VaultControl is not { } vault)
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

    public VaultControl? VaultControl { get; set; }

    private Encoding utf8 = new UTF8Encoding(true, false);
}
