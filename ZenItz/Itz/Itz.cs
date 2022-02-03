// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public void Set<T>(Identifier primaryId, Identifier secondaryId, ref T value)
        where T : struct
    {
        var ship = ItzShipResolver.Instance.GetShip<T>();
    }

    public void Get<T>(Identifier primaryId, Identifier secondaryId, ref T value)
        where T : struct
    {
    }
}
