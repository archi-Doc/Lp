// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere;

public class NetService
{
    public delegate NetResult ServiceDelegate(object instance, ByteArrayPool.MemoryOwner received, out ByteArrayPool.MemoryOwner send);

    public delegate INetService CreateClientDelegate(ClientTerminal clientTerminal);

    public delegate object CreateServerDelegate(IServiceProvider? serviceProvider);

    public class ServiceInfo
    {
        public ServiceInfo(uint serviceId, CreateServerDelegate createServer)
        {
            this.ServiceId = serviceId;
            this.CreateServer = createServer;
        }

        public uint ServiceId { get; }

        public CreateServerDelegate CreateServer { get; }

        public Dictionary<ulong, ServiceMethod> ServiceMethods { get; } = new();
    }

    public class ServiceMethod
    {
        public ServiceMethod(ulong id, ServiceDelegate process)
        {
            this.Id = id;
            this.Process = process;
        }

        // public ServiceInfo ServiceInfo { get; }

        public ulong Id { get; }

        public object? ServerInstance { get; private set; }

        public ServiceDelegate Process { get; }

        public ServiceMethod CloneWithInstance(object serverInstance)
        {
            var serviceMethod = new ServiceMethod(this.Id, this.Process);
            serviceMethod.ServerInstance = serverInstance;
            return serviceMethod;
        }
    }

    public NetService()
    {
    }

    public async Task<NetResult> Process(ServerTerminal serverTerminal, NetReceivedData received)
    {
        if (!this.idToServiceMethod.TryGetValue(received.DataId, out var serviceMethod))
        {
            var serviceId = (uint)(received.DataId >> 32);
            if (!StaticNetService.TryGetServiceInfo(serviceId, out var serviceInfo))
            {
                return NetResult.NoNetService;
            }

            if (!serviceInfo.ServiceMethods.TryGetValue(received.DataId, out serviceMethod))
            {
                return NetResult.NoNetService;
            }

            if (!this.idToInstance.TryGetValue(serviceId, out var serverInstance))
            {
                serverInstance = serviceInfo.CreateServer(null);
                this.idToInstance.TryAdd(serviceId, serverInstance);
            }

            serviceMethod = serviceMethod.CloneWithInstance(serverInstance);
        }

        var result = serviceMethod.Process(serviceMethod.ServerInstance!, received.Received, out var sendOwner);
        if (result != NetResult.Success)
        {
            return result;
        }

        result = await serverTerminal.SendDataAsync(serviceMethod.Id, sendOwner);
        sendOwner.Return();
        return result;
    }

    private Dictionary<ulong, ServiceMethod> idToServiceMethod = new();

    private Dictionary<uint, object> idToInstance = new();
}
