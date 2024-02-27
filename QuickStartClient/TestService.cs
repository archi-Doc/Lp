// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace QuickStart;

[NetServiceInterface] // Annotate NetServiceInterface attribute.
public interface ITestService : INetService // The interface for NetService must inherit from INetService.
{
    NetTask<string?> DoubleString(string input); // Declare the service method.
    // Ensure that both arguments and return values are serializable by Tinyhand serializer, and the return type must be NetTask or NetTask<T>.
}
