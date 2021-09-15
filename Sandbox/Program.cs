// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DryIoc;
using LP;
using LP.Zen;

namespace Sandbox;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Sandbox");

        var container = new Container();
        LPCore.Register(container);

        container.ValidateAndThrow();
    }
}
