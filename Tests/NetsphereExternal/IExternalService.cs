using Netsphere;

namespace NetsphereTest;

[NetServiceInterface]
public interface IExternalService : INetService
{
    public NetTask SendExternal(int x);

    public NetTask<int> IncrementExternal(int x);

    public NetTask<NetResult> SendExternal(int x, string y);
}
