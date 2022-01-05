using Netsphere;

namespace NetsphereTest;

[NetServiceInterface]
public interface IExternalService : INetService
{
    public Task SendExternal(int x);

    public Task<int> IncrementExternal(int x);
}
