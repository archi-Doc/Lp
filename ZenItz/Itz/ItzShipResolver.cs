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
            IdToType.Add(id, typeof(TPayload));
        }
    }

    internal static ulong TypeToId<TPayload>()
    {
        return Arc.Crypto.FarmHash.Hash64(typeof(TPayload).FullName ?? string.Empty);
    }

    internal byte[] Serialize()
    {
        var writer = default(Tinyhand.IO.TinyhandWriter);
        byte[]? byteArray;
        try
        {
            foreach (var x in IdToShip)
            {
                var span = writer.GetSpan(12); // Id + Length
                writer.Advance(12);

                var written = writer.Written;
                x.Value.Serialize(ref writer);

                BitConverter.TryWriteBytes(span, x.Key);
                span = span.Slice(8);
                BitConverter.TryWriteBytes(span, (int)(writer.Written - written));
            }

            byteArray = writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }

        return byteArray;
        // return TinyhandSerializer.Serialize(IdToShip);
    }

    internal bool Deserialize(byte[] byteArray)
    {
        var memory = byteArray.AsMemory();
        try
        {
            while (memory.Length >= 12)
            {
                var id = BitConverter.ToUInt64(memory.Span);
                memory = memory.Slice(8);
                var length = BitConverter.ToInt32(memory.Span);
                memory = memory.Slice(4);

                if (IdToShip.TryGetValue(id, out var ship))
                {
                    ship.Deserialize(memory, out _);
                }

                memory = memory.Slice(length);
            }
        }
        catch
        {
            return false;
        }

        return true;
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

    private static readonly Dictionary<Type, IItzShip> TypeToShip = new();
    private static readonly Dictionary<ulong, IItzShip> IdToShip = new();
    private static readonly Dictionary<ulong, Type> IdToType = new();
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
