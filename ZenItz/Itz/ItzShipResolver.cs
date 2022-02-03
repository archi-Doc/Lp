// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

internal class ItzShipResolver
{
    public static readonly ItzShipResolver Instance = new ();

    private ItzShipResolver()
    {
    }

    public IItzShip<T>? TryGet<T>()
        where T : struct
    {
        return ShipCache<T>.Ship;
    }

    public void Register<T>(IItzShip<T> ship)
        where T : struct
    {
        TypeToShip.TryAdd(typeof(T), ship);
    }

    private static class ShipCache<T>
        where T : struct
    {
        public static readonly IItzShip<T>? Ship;

        static ShipCache()
        {
            if (ItzShipResolver.TypeToShip.TryGetValue(typeof(T), out var obj))
            {
                ShipCache<T>.Ship = (IItzShip<T>)obj;
            }
        }
    }

    private static readonly Dictionary<Type, object> TypeToShip = new();
}

public static class ResolverExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IItzShip<T> GetShip<T>(this ItzShipResolver resolver)
        where T : struct
    {
        IItzShip<T>? ship;

        ship = resolver.TryGet<T>();
        if (ship == null)
        {
            throw new InvalidOperationException(typeof(T).FullName + " is not registered in resolver: " + resolver.GetType());
        }

        return ship!;
    }
}
