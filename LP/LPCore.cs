// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using BigMachines;
using DryIoc;
using LP;
using LP.Net;

namespace LP;

public class LPCore
{
    public static void Register(Container container)
    {
        // Base
        container.Register<Hash>(Reuse.Transient);
        container.RegisterDelegate(x => new BigMachine<Identifier>(ThreadCore.Root, container), Reuse.Singleton);

        // Main services
        container.Register<LPCore>(Reuse.Singleton);
        container.Register<Netsphere>(Reuse.Singleton);
    }

    public LPCore(Netsphere netsphere)
    {
        this.Core = new(ThreadCore.Root);
        this.Netsphere = netsphere;
    }

    public void Initialize()
    {
    }

    public void Terminate()
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);
    }

    public ThreadCoreGroup Core { get; }

    public Netsphere Netsphere { get; }
}
