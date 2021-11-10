// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleCommandLine;

namespace LPConsole
{
    [SimpleCommand("test2")]
    public class TestCommand2 : ISimpleCommand
    {
        public void Run(string[] args)
        {
            Console.WriteLine("Test command2:");
        }
    }
}
