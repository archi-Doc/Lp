// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Datum;

namespace CrystalData;

public class DatumConstructor
{
    public const int DefaultMaxId = 10;

    public delegate IBaseDatum ConstrutorDelegate(IDataInternal fromDataToIO);

    public DatumConstructor()
    {
        this.MaxId = DefaultMaxId;
        this.constructors = new ConstrutorDelegate[this.MaxId];
    }

    public bool Register<TDatum>(ConstrutorDelegate construtor)
        where TDatum : IDatum
    {
        var id = TDatum.StaticId;
        if (id < 0 || id >= this.constructors.Length)
        {
            return false;
        }

        this.constructors[id] = construtor;
        return true;
    }

    public void Resize(int max)
    {
        Array.Resize(ref this.constructors, max);
    }

    public int MaxId { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ConstrutorDelegate? TryGetConstructor(int id)
    {
        if (id < 0 || id >= this.constructors.Length)
        {
            return null;
        }

        return this.constructors[id];
    }

    private ConstrutorDelegate[] constructors;
}
