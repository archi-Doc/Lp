// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public class Itz
{
    public Itz()
    {
    }

    public void Register<T>(IItzShip<T> ship)
        where T : struct
    {
        ItzShipResolver.Instance.Register<T>(ship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IItzShip<T> GetShip<T>()
        where T : struct
        => ItzShipResolver.Instance.GetShip<T>();

    public void Set<T>(Identifier primaryId, Identifier? secondaryId, ref T value)
        where T : struct
        => this.GetShip<T>().Set(primaryId, secondaryId, ref value);

    public ItzResult Get<T>(Identifier primaryId, Identifier? secondaryId, out T value)
        where T : struct
        => this.GetShip<T>().Get(primaryId, secondaryId, out value);
}
