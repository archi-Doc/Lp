﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace QuickStart;

// Define an interface shared between the client and server.
[NetServiceInterface] // Annotate NetServiceInterface attribute.
public interface ITestService : INetService // An interface for NetService must inherit from INetService.
{
    NetTask<string?> DoubleString(string input); // Declare the service method.
    // Ensure that both arguments and return values are serializable by Tinyhand serializer, and the return type must be NetTask or NetTask<T>.
}

[NetServiceInterface]
public interface ITestService2 : INetService
{
    NetTask<int> Random();
}
