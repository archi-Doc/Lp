// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Netsphere;

public sealed class ServiceControl
{
    public ServiceControl()
    {
    }

    private UInt32Hashtable<ServerConnectionContext.AgentInfo?> serviceIdToAgentInfo = new();

    public void Register<TService, TAgent>()
        where TService : INetService
        where TAgent : class, TService
    {
        var serviceId = ServiceTypeToId<TService>();
        this.Register(serviceId, typeof(TAgent));
    }

    public void Register(Type serviceType, Type agentType)
    {
        var serviceId = ServiceTypeToId(serviceType);
        this.Register(serviceId, agentType);
    }

    public void Unregister<TService>()
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        this.serviceIdToAgentInfo.TryAdd(serviceId, default);//
    }

    public void Unregister(Type serviceType)
    {
        var serviceId = ServiceTypeToId(serviceType);
        this.serviceIdToAgentInfo.TryAdd(serviceId, default);//
    }

    public bool TryGet<TService>([MaybeNullWhen(false)] out ServerConnectionContext.AgentInfo info)
        where TService : INetService
    {
        var serviceId = ServiceTypeToId<TService>();
        return this.TryGet(serviceId, out info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(uint serviceId, [MaybeNullWhen(false)] out ServerConnectionContext.AgentInfo info)
        => this.serviceIdToAgentInfo.TryGetValue(serviceId, out info);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ServiceTypeToId<TService>()
        where TService : INetService
        => (uint)FarmHash.Hash64(typeof(TService).FullName ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ServiceTypeToId(Type serviceType)
        => (uint)FarmHash.Hash64(serviceType.FullName ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Register(uint serviceId, Type agentType)
    {
        if (!StaticNetService.TryGetAgentInfo(agentType, out var info))
        {
            throw new InvalidOperationException("Failed to register the class with the corresponding ServiceId.");
        }

        this.serviceIdToAgentInfo.TryAdd(serviceId, info);
    }
}
