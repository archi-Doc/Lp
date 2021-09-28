// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace LP;

public static class Message
{
    public class Configure
    {
    }

    public class SerializeAsync
    {
    }

    public record DeserializeAsync(CancellationToken CancellationToken);

    public class Start
    {
    }

    public class Stop
    {
    }
}
