// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IStorageKey
{
    bool TryGetKey(string bucket, out AccessKeyPair accessKeyPair);
}
