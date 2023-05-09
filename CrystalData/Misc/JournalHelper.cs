// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData.Journal;

public static class JournalHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Locator(this TinyhandWriter writer)
        => writer.Write((byte)LocatorKeyValue.Locator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Key(this TinyhandWriter writer)
        => writer.Write((byte)LocatorKeyValue.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Value(this TinyhandWriter writer)
        => writer.Write((byte)LocatorKeyValue.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LocatorKeyValue Read_LocatorKeyValue(this TinyhandReader reader)
       => (LocatorKeyValue)reader.ReadUInt8();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Locator(this TinyhandReader reader)
    {
        if (reader.Read_LocatorKeyValue() != LocatorKeyValue.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Key(this TinyhandReader reader)
    {
        if (reader.Read_LocatorKeyValue() != LocatorKeyValue.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Value(this TinyhandReader reader)
    {
        if (reader.Read_LocatorKeyValue() != LocatorKeyValue.Value)
        {
            throw new InvalidDataException();
        }
    }
}
