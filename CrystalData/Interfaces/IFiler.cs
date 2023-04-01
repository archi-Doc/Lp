// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IFiler<T>
    where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
{
}
