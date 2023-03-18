// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal interface IFiler
{
    TaskWorkInterface<FilerWork> Get(string path, int sizeToGet);

    TaskWorkInterface<FilerWork> Put(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared);

    TaskWorkInterface<FilerWork> Delete(string path);
}
