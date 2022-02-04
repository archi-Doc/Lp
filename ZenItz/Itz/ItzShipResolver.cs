// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

internal class ItzShipResolver
{
    public static readonly ItzShipResolver Instance = new ();

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
        TypeToShip.TryAdd(typeof(TPayload), ship);
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

    private static readonly Dictionary<Type, object> TypeToShip = new();
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
