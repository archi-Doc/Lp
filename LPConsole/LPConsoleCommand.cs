﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Arc.Crypto;
using Arc.Threading;
using CrossChannel;
using LP;
using LP.Options;
using LP.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;
using ZenItz;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole;

[SimpleCommand("lp", Default = true)]
public class LPConsoleCommand : ISimpleCommandAsync<LPOptions>
{
    public LPConsoleCommand(Control.Unit unit)
    {
        this.unit = unit;
    }

    public async Task Run(LPOptions options, string[] args)
    {
        await this.unit.Run(options);
    }

    /*
    private async Task<AbortOrComplete> LoadNodeKey()
    {
        var file = NodePrivateKey.Filename;
        if (!File.Exists(file))
        {
            return AbortOrComplete.Complete;
        }

        try
        {
            var encrypted = File.ReadAllBytes(file);
            Memory<byte> data;
            if (PasswordEncrypt.TryDecrypt(encrypted, string.Empty, out data))
            {
                goto Deserialize;
            }

            Console.WriteLine(file);
EnterNodeKeyPassword:
            var password = this.GetPassword();
            if (password == null)
            {
                Console.WriteLine();
                return AbortOrComplete.Abort;
            }
            else if (!PasswordEncrypt.TryDecrypt(encrypted, password, out data))
            {// Incorrect
                Console.WriteLine("Incorrect password.");
                Console.WriteLine();
                goto EnterNodeKeyPassword;
            }
            else
            {// Correct
                Console.WriteLine();
            }

Deserialize:
            var key = Tinyhand.TinyhandSerializer.Deserialize<NodePrivateKey>(data);
            if (key != null)
            {
                this.netBase.SetNodeKey(key);
                Logger.Default.Information($"Loaded: {file}");
            }
        }
        catch
        {
        }

        return AbortOrComplete.Complete;
    }*/

    private Control.Unit unit;
}
