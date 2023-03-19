// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal interface IFiler
{
    IAbortOrCompleteTask? Get(string path, int sizeToGet);

    IAbortOrCompleteTask? Put(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared);

    IAbortOrCompleteTask? Delete(string path);
}
