// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace LP;

public static class Message
{// Create instance -> Configure -> SerializeAsync -> Start/Stop loop -> DeserializeAsync
    public class Configure
    {
    }

    public class SerializeAsync
    {
    }

    public record DeserializeAsync();

    public class Start
    {
    }

    public class Stop
    {
    }
}
