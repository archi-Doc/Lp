// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CrystalData.Datum;

namespace CrystalData;

public class DatumRegistry
{
    internal readonly struct DatumInfo
    {
        public DatumInfo(ushort datumId, ConstructorDelegate constructor)
        {
            this.DatumId = datumId;
            this.Constructor = constructor;
        }

        public readonly ushort DatumId;
        public readonly ConstructorDelegate Constructor;
    }

    public delegate IBaseDatum ConstructorDelegate(IDataInternal dataInternal);

    /// <summary>
    /// Initializes a new instance of the <see cref="DatumRegistry"/> class.<br/>
    ///  <see cref="DatumRegistry"/> manages types of Datum used in the Crystal.
    /// </summary>
    public DatumRegistry()
    {// Consider FrozenDictionary.
    }

    /// <summary>
    /// Registers the type, id, and constructor of a Datum, intended to be called within the constructor of Crystal.<br/>
    /// Datum id must be above 0.
    /// </summary>
    /// <typeparam name="TDatum">The type of datum.</typeparam>
    /// <param name="datumId">The identifier of datum. Must be above 0.</param>
    /// <param name="construtor">The constructor of datum.</param>
    /// <exception cref="ArgumentOutOfRangeException">Datum id is 0 (invalid) or already registered.</exception>
    public void Register<TDatum>(ushort datumId, ConstructorDelegate construtor)
        where TDatum : IDatum
    {
        if (datumId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(datumId), "Datum id must be above 0");
        }
        else if (this.datumIdToDatumInfo.TryGetValue(datumId, out var datumInfo))
        {
            /*if (datumInfo.DatumId == datumId)
            {// Identical
                return;
            }*/

            throw new ArgumentOutOfRangeException(nameof(datumId), "The same datum id is already registered");
        }

        var info = new DatumInfo(datumId, construtor);
        if (!this.typeToDatumInfo.TryAdd(typeof(TDatum), info))
        {
            throw new ArgumentOutOfRangeException(nameof(TDatum), "The same datum type is already registered");
        }

        this.datumIdToDatumInfo.TryAdd(datumId, info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetDatumInfo<TDatum>(out DatumInfo info)
        where TDatum : IDatum
    {
        if (this.typeToDatumInfo.TryGetValue(typeof(TDatum), out info))
        {
            return true;
        }

        info = default;
        return false;
    }

    private ThreadsafeTypeKeyHashTable<DatumInfo> typeToDatumInfo = new();
    private UInt16Hashtable<DatumInfo> datumIdToDatumInfo = new();
}
