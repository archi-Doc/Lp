using Netsphere.Server;

namespace NetsphereTest;

[NetServiceInterface]
public interface ICustomService : INetService
{
    public NetTask Test();
}

[NetServiceInterface]
public interface ICustomService2 : INetService
{
    public NetTask Test();
}

[NetServiceObject]
[NetServiceFilter<CustomFilter>(Order = 0)]
public class CustomService : ICustomService, ICustomService2
{
    [NetServiceFilter<CustomFilter>(Arguments = new object[] { 1, 2, new string?[] { "te" }, 3 })]
    async NetTask ICustomService.Test()
    {
    }

    [NetServiceFilter<CustomFilter>(Arguments = new object[] { 9, })]

    public async NetTask Test()
    {
    }
}

public class CustomFilter : IServiceFilter
{
    public async Task Invoke(TransmissionContext context, Func<TransmissionContext, Task> invoker)
    {
        if (context is not TransmissionContext testContext)
        {
            throw new NetException(NetResult.UnknownException);
        }

        await invoker(context).ConfigureAwait(false);
    }
    /*public async Task Invoke(CallContext context, Func<CallContext, Task> invoker)
    {
        if (context is not TestCallContext testContext)
        {
            throw new NetException(NetResult.NoCallContext);
        }

        await invoker(context).ConfigureAwait(false);
    }*/

    public void SetArguments(object[] args)
    {
    }
}
