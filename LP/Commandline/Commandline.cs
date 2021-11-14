// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP;

public class Commandline
{
    public Commandline(Netsphere netsphere)
    {
        this.Netsphere = netsphere;
    }

    public bool Process(string command)
    {
        if (string.Compare(command, "gc", true) == 0)
        {
            GC.Collect();
            System.Console.WriteLine("GC Collected");
            return true;
        }
        else if (string.Compare(command, "a", true) == 0)
        {
            System.Console.WriteLine(this.Netsphere.Terminal.NetTerminals.QueueChain.Count());
            return true;
        }

        return false;
    }

    public Netsphere Netsphere { get; }
}
