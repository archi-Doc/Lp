// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Netsphere;

public static class StaticNetService
{
    public static void SetClientDelegate<TService>(NetService.CreateClientDelegate @delegate)
        where TService : INetService
    {
        DelegateCache<TService>.Create = @delegate;
    }

    public static bool TryGetServiceInfo(uint id, [MaybeNullWhen(false)] out NetService.ServiceInfo info)
    {
        return idToInfo.TryGetValue(id, out info);
    }

    public static TService CreateClient<TService>(ClientTerminal clientTerminal)
        where TService : INetService
    {
        var create = DelegateCache<TService>.Create;
        if (create != null && create(clientTerminal) is TService service)
        {
            return service;
        }

        throw new InvalidOperationException($"Could not create an instance of net service {typeof(TService)}.");
    }

    private static ConcurrentDictionary<uint, NetService.ServiceInfo> idToInfo = new();

    private static class DelegateCache<T>
    {
#pragma warning disable SA1401 // Fields should be private
        internal static NetService.CreateClientDelegate? Create;
#pragma warning restore SA1401 // Fields should be private

        static DelegateCache()
        {
        }
    }
}
