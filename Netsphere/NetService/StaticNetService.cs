﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Netsphere.Server;

namespace Netsphere;

public static class StaticNetService
{
    public static void SetFrontendDelegate<TService>(ConnectionContext.CreateFrontendDelegate @delegate)
        where TService : INetService
    {
        DelegateCache<TService>.Create = @delegate;
    }

    public static void SetServiceInfo(ConnectionContext.ServiceInfo info)
        => serviceIdToInfo.TryAdd(info.ServiceId, info); // serviceIdToInfo[info.ServiceId] = info;

    public static bool TryGetServiceInfo(uint serviceId, [MaybeNullWhen(false)] out ConnectionContext.ServiceInfo info)
        => serviceIdToInfo.TryGetValue(serviceId, out info);

    public static TService CreateClient<TService>(ClientConnection clientConnection)
        where TService : INetService
    {
        var create = DelegateCache<TService>.Create;
        if (create != null && create(clientConnection) is TService service)
        {
            return service;
        }

        throw new InvalidOperationException($"Could not create an instance of the net service {typeof(TService).ToString()}.");
    }

    private static UInt32Hashtable<ConnectionContext.ServiceInfo> serviceIdToInfo = new();

    private static class DelegateCache<T>
    {
#pragma warning disable SA1401 // Fields should be private
        internal static ConnectionContext.CreateFrontendDelegate? Create;
#pragma warning restore SA1401 // Fields should be private

        static DelegateCache()
        {
        }
    }
}
