// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public class ZenData
{
    public const int DefaultMaxId = 10;

    public delegate IBaseData ConstrutorDelegate(IFlakeInternal fromDataToIO);

    public ZenData()
    {
        this.MaxId = DefaultMaxId;
        this.constructors = new ConstrutorDelegate[this.MaxId];
    }

    public bool Register<TData>(ConstrutorDelegate construtor)
        where TData : IData
    {
        var id = TData.StaticId;
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
