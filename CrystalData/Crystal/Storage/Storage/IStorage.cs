// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData.Storager;

internal interface IStorage
{
    bool PrepareAndCheck(StorageClass storage);

    IAbortOrCompleteTask? Get(ulong fileId);

    IAbortOrCompleteTask? Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared);

    IAbortOrCompleteTask? Delete(ref ulong fileId);
}
