// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Threading;

namespace LP;

public static class Message
{// Create instance -> Configure -> LoadAsync -> Start/Stop loop -> SaveAsync
    public record Configure();

    public record SaveAsync();

    public record LoadAsync();

    public record Start(ThreadCoreBase ParentCore);

    public record Stop();
}
