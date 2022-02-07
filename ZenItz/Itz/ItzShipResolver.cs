// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

internal class ItzShipResolver
{
    public static readonly ItzShipResolver Instance = new();

    private ItzShipResolver()
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
            // IdToType.Add(id, typeof(TPayload));
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
            if (ItzShipResolver.TypeToShip.TryGetValue(typeof(TPayload), out var obj))
            {
                ShipCache<TPayload>.Ship = (IItzShip<TPayload>)obj;
            }
        }
    }

    internal static readonly Dictionary<ulong, ITinyhandSerializable> IdToShip = new();
    private static readonly Dictionary<Type, IItzShip> TypeToShip = new();
    // private static readonly Dictionary<ulong, Type> IdToType = new();
}

public static class ResolverExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IItzShip<TPayload> GetShip<TPayload>(this ItzShipResolver resolver)
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
