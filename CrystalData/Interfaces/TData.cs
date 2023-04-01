// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IStorage<TData>
    where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
}
