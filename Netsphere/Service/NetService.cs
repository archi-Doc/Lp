// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere;

public class NetService
{
    public delegate bool ServiceDelegate(object instance, ByteArrayPool.MemoryOwner received, out ByteArrayPool.MemoryOwner send);

    public delegate INetService CreateClientDelegate(ClientTerminal clientTerminal);

    public delegate object CreateServerDelegate(IServiceProvider? serviceProvider);

    public class ServiceInfo
    {
        public CreateServerDelegate CreateServer { get; }
    }

    public class MethodInfo
    {
        public MethodInfo(ServiceInfo serviceInfo, object serverInstance)
        {
            this.ServiceInfo = serviceInfo;
            this.ServerInstance = serverInstance;
        }

        public ServiceInfo ServiceInfo { get; }

        public ulong Id { get; }

        public object ServerInstance { get; }

        public ServiceDelegate Process { get; }
    }

    public NetService()
    {
    }

    public async Task<bool> Process(ServerTerminal serverTerminal, NetReceivedData received)
    {
        if (!this.idToMethodInfo.TryGetValue(received.DataId, out var methodInfo))
        {
            var serviceId = (uint)(received.DataId >> 32);
            if (!StaticNetService.TryGetServiceInfo(serviceId, out var serviceInfo))
            {
                return false;
            }

            if (!this.idToInstance.TryGetValue(serviceId, out var serverInstance))
            {
                serverInstance = serviceInfo.CreateServer(null);
                this.idToInstance.TryAdd(serviceId, serverInstance);
            }

            methodInfo = new MethodInfo(serviceInfo, serverInstance);
        }

        if (!methodInfo.Process(methodInfo.ServerInstance, received.Received, out var sendOwner))
        {
            return false;
        }

        await serverTerminal.SendDataAsync(methodInfo.Id, sendOwner);
        sendOwner.Return();
        return true;
    }

    private Dictionary<ulong, MethodInfo> idToMethodInfo = new();

    private Dictionary<uint, object> idToInstance = new();
}
