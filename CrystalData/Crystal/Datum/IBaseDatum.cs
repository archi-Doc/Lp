// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Datum;

public interface IBaseDatum
{
    /// <summary>
    /// Serialize a datum and store it in a storage.
    /// </summary>
    void Save();

    /// <summary>
    /// Free resources (e.g. memory).
    /// </summary>
    void Unload();
}
