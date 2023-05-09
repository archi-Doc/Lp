﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData.Journal;

public static class JournalHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Locator(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Locator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Key(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Value(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Add(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Add);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Remove(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Remove);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write_Clear(this TinyhandWriter writer)
        => writer.Write((byte)JournalRecord.Clear);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JournalRecord Read_Record(this TinyhandReader reader)
       => (JournalRecord)reader.ReadUInt8();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Locator(this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Locator)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Key(this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Key)
        {
            throw new InvalidDataException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read_Value(this TinyhandReader reader)
    {
        if (reader.Read_Record() != JournalRecord.Value)
        {
            throw new InvalidDataException();
        }
    }
}