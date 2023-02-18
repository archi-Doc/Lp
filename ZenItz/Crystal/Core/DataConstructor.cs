// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData;

namespace ZenItz.Crystal.Core;

public class DataConstructor
{
    public const int DefaultMaxId = 10;

    public delegate IBaseData ConstrutorDelegate(IFlakeInternal fromDataToIO);

    public DataConstructor()
    {
        MaxId = DefaultMaxId;
        constructors = new ConstrutorDelegate[MaxId];
    }

    public bool Register<TData>(ConstrutorDelegate construtor)
        where TData : IDatum
    {
        var id = TData.StaticId;
        if (id < 0 || id >= constructors.Length)
        {
            return false;
        }

        constructors[id] = construtor;
        return true;
    }

    public void Resize(int max)
    {
        Array.Resize(ref constructors, max);
    }

    public int MaxId { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ConstrutorDelegate? TryGetConstructor(int id)
    {
        if (id < 0 || id >= constructors.Length)
        {
            return null;
        }

        return constructors[id];
    }

    private ConstrutorDelegate[] constructors;
}
