﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
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
    {
        idToInfo[info.ServiceId] = info;
    }

    public static bool TryGetServiceInfo(uint id, [MaybeNullWhen(false)] out ConnectionContext.ServiceInfo info)
    {
        return idToInfo.TryGetValue(id, out info);
    }

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

    private static ConcurrentDictionary<uint, ConnectionContext.ServiceInfo> idToInfo = new();

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