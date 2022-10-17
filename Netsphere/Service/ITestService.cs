// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace NetsphereTest;

[NetServiceInterface]
public interface IRemoteControlService : INetService
{
    public NetTask RequestAuthorization(Token token);

    public NetTask Restart();
}
