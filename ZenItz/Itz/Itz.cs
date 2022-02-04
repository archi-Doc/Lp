// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public class Itz
{
    public Itz()
    {
    }

    public void Register<TPayload>(IItzShip<TPayload> ship)
        where TPayload : IItzPayload
    {
        ItzShipResolver.Instance.Register<TPayload>(ship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IItzShip<TPayload> GetShip<TPayload>()
        where TPayload : IItzPayload
        => ItzShipResolver.Instance.GetShip<TPayload>();

    public void Set<TPayload>(Identifier primaryId, Identifier? secondaryId, ref TPayload value)
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Set(primaryId, secondaryId, ref value);

    public ItzResult Get<TPayload>(Identifier primaryId, Identifier? secondaryId, out TPayload value)
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Get(primaryId, secondaryId, out value);

    public byte[] Serialize<TPayload>()
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Serialize();

    public void Deserialize<TPayload>(ReadOnlyMemory<byte> memory)
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Deserialize(memory);
}
