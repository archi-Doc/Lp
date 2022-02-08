// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

internal class ItzShipControl
{
    public static readonly ItzShipControl Instance = new();

    private ItzShipControl()
    {
    }

    public IItzShip<TPayload>? TryGet<TPayload>()
        where TPayload : IItzPayload
    {
        return ShipCache<TPayload>.Ship;
    }

    public void Register<TPayload>(IItzShip<TPayload> ship)
        where TPayload : IItzPayload
    {
        if (TypeToShip.TryAdd(typeof(TPayload), ship))
        {
            var id = TypeToId<TPayload>();
            IdToShip.Add(id, ship);
        }
    }

    internal static ulong TypeToId<TPayload>()
    {
        return Arc.Crypto.FarmHash.Hash64(typeof(TPayload).FullName ?? string.Empty);
    }

    private static class ShipCache<TPayload>
        where TPayload : IItzPayload
    {
        public static readonly IItzShip<TPayload>? Ship;

        static ShipCache()
        {
            if (ItzShipControl.TypeToShip.TryGetValue(typeof(TPayload), out var obj))
            {
                ShipCache<TPayload>.Ship = (IItzShip<TPayload>)obj;
            }
        }
    }

    internal static readonly Dictionary<ulong, ILPSerializable> IdToShip = new();
    private static readonly Dictionary<Type, IItzShip> TypeToShip = new();
}

public static class ResolverExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IItzShip<TPayload> GetShip<TPayload>(this ItzShipControl resolver)
        where TPayload : IItzPayload
    {
        IItzShip<TPayload>? ship;

        ship = resolver.TryGet<TPayload>();
        if (ship == null)
        {
            throw new InvalidOperationException(typeof(TPayload).FullName + " is not registered in resolver: " + resolver.GetType());
        }

        return ship!;
    }
}
