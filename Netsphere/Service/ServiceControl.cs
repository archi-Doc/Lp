// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Netsphere;

public sealed class ServiceControl
{
    public ServiceControl()
    {
    }

    private UInt32Hashtable<ServerConnectionContext.ServiceInfo> serviceIdToServiceInfo = new();

    public void Register<TService, TAgent>()
        where TService : INetService
        where TAgent : class, TService
    {
        var serviceId = ServiceTypeToId<TService>();
        this.Register(serviceId);
    }

    public void Register(Type serviceType, Type agentType)
    {
        var serviceId = ServiceTypeToId(serviceType);
        this.Register(serviceId);
    }

    public void Register<TService>()
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        this.Register(serviceId);
    }

    public void Register(Type serviceType)
    {
        var serviceId = ServiceTypeToId(serviceType);
        this.Register(serviceId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(uint serviceId)
    {
        if (!StaticNetService.TryGetServiceInfo(serviceId, out var info))
        {
            throw new InvalidOperationException("Failed to register the class with the corresponding ServiceId.");
        }

        this.serviceIdToServiceInfo.TryAdd(serviceId, info);
    }

    public bool TryGet<TService>([MaybeNullWhen(false)] out ServerConnectionContext.ServiceInfo info)
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        return this.TryGet(serviceId, out info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(uint serviceId, [MaybeNullWhen(false)] out ServerConnectionContext.ServiceInfo info)
        => this.serviceIdToServiceInfo.TryGetValue(serviceId, out info);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ServiceTypeToId<TService>()
        where TService : INetService
        => (uint)Arc.Crypto.FarmHash.Hash64(typeof(TService).FullName ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ServiceTypeToId(Type serviceType)
        => (uint)Arc.Crypto.FarmHash.Hash64(serviceType.FullName ?? string.Empty);
}
