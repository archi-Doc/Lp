// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData.Storager;

internal interface IStorager
{
    Task<CrystalMemoryOwnerResult> Get(ulong fileId);

    TaskWorkInterface<FilerWork>? Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    TaskWorkInterface<FilerWork>? Delete(ref ulong fileId);
}
