// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Unload;

public interface IUnloadable
{
    bool Save(UnloadMode unloadMode);
}
