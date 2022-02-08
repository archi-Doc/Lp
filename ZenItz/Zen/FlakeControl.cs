// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class FlakeControl
{
    public static readonly FlakeControl Instance = new();

    public FlakeControl()
    {
    }

    public int GetFlakeId()
    {
        return 0;
    }

    private Flake[] flakeArray;
}
