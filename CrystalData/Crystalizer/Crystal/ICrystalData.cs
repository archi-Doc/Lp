// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData;

public interface ICrystalData
{
    public ICrystal? Crystal { get; protected set; }

    public uint CurrentPlane { get; protected set; }

    public void WriteLocator(ref TinyhandWriter writer);

    public void ReadRecord(ref TinyhandReader reader);
}
