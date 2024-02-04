// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Netsphere.Server;

namespace Netsphere;

public sealed class ServiceControl
{
    public ServiceControl()
    {
    }

    private UInt64Hashtable<ConnectionContext.ServiceInfo> dataIdToResponder = new();

    public bool Register<TService>()
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        return this.Register(serviceId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Register(uint serviceId)
    {
        if (StaticNetService.TryGetServiceInfo(serviceId, out var info))
        {
            this.dataIdToResponder.TryAdd(serviceId, info);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryGet<TService>([MaybeNullWhen(false)] out ConnectionContext.ServiceInfo info)
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        return this.TryGet(serviceId, out info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(uint serviceId, [MaybeNullWhen(false)] out ConnectionContext.ServiceInfo info)
        => this.dataIdToResponder.TryGetValue(serviceId, out info);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ServiceTypeToId<TService>()
        where TService : INetService
        => (uint)Arc.Crypto.FarmHash.Hash64(typeof(TService).FullName ?? string.Empty);
}
