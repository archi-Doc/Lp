// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Unload;

public interface IUnloadable
{
    /// <summary>
    /// Serialize data and store it in a storage.
    /// </summary>
    void Save();

    /// <summary>
    /// Unload data (e.g. memory).
    /// </summary>
    void Unload();
}
