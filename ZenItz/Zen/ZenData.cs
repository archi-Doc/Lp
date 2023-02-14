// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public record ZenDataInformation(int Id, Func<BaseData> CreateInstance);

public static class ZenData
{
    public const int MaxId = 10;

    public delegate object ConstrutorDelegate(ZenOptions options, IFromDataToIO fromDataToIO);

    public static bool Register<TData>(ConstrutorDelegate construtor)
        where TData : IData
    {
        var id = TData.StaticId;
        if (id < 0 || id >= MaxId)
        {
            return false;
        }

        constructors[id] = construtor;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ConstrutorDelegate? TryGetConstructor(int id)
    {
        if (id < 0 || id >= MaxId)
        {
            return null;
        }

        return constructors[id];
    }

    private static ConstrutorDelegate[] constructors = new ConstrutorDelegate[MaxId];
}
