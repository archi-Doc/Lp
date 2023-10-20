// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace CrystalData;

/*[TinyhandObject]
public partial class CrystalDataInterface : ITinyhandSerialize<CrystalDataInterface>, ITinyhandReconstruct<CrystalDataInterface>, ITreeObject
{
    public CrystalDataInterface()
    {
    }

    static void ITinyhandSerialize<CrystalDataInterface>.Serialize(ref TinyhandWriter writer, scoped ref CrystalDataInterface? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteSpan(value.data);
    }

    static void ITinyhandSerialize<CrystalDataInterface>.Deserialize(ref TinyhandReader reader, scoped ref CrystalDataInterface? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        value.data = reader.ReadRaw(reader.Remaining).ToArray(); // tempcode
    }

    static void ITinyhandReconstruct<CrystalDataInterface>.Reconstruct([NotNull] scoped ref CrystalDataInterface? value, TinyhandSerializerOptions options)
    {
        value ??= new();
    }

    private byte[] data = Array.Empty<byte>();

    public byte[] Data
    {
        get => this.data;
        set
        {
            this.data = value;
            this.TryAddToSaveQueue();
        }
    }

    public bool TryAddToSaveQueue()
        => ((ITreeObject)this).TreeRoot?.TryAddToSaveQueue() == true;

    [IgnoreMember]
    ITreeRoot? ITreeObject.TreeRoot { get; set; }

    [IgnoreMember]
    ITreeObject? ITreeObject.TreeParent { get; set; }

    [IgnoreMember]
    int ITreeObject.TreeKey { get; set; } = -1;

    void ITreeObject.SetParent(ITreeObject? parent, int key)
    {
        ((ITreeObject)this).SetParentActual(parent, key);
    }

    bool ITreeObject.ReadRecord(ref TinyhandReader reader)
    {
        return false;
    }
}
*/
