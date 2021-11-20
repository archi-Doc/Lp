// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP;

internal class PassiveTerminal
{
    public PassiveTerminal(Information information)
    {
        this.Information = information;
    }

    public void Process(NetTerminal terminal)
    {
    }

    public Information Information { get; }
}
