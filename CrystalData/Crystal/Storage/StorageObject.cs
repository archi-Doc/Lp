// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;
using CrystalData.Storage;

namespace CrystalData;

[TinyhandObject]
[ValueLinkObject]
internal partial class StorageObject
{
    public StorageObject()
    {
    }

    [IgnoreMember]
    public IStorage? Storage { get; set; }

    [IgnoreMember]
    public IFiler? Filer { get; set; }

    [Key(0)]
    [Link(Type = ChainType.Unordered, Primary = true, NoValue = true)]
    public ushort StorageId { get; set; }

    [Key(1)]
    public byte[] FilerData { get; set; } = default!;

    [Key(2)]
    public MemoryStat MemoryStat { get; private set; } = default!;
}
