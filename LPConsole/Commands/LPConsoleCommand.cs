// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arc.Crypto;
using Arc.Threading;
using DryIoc;
using LP;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole;

[SimpleCommand("lp", Default = true)]
public class LPConsoleCommand : ISimpleCommandAsync<LPConsoleOptions>
{
    public LPConsoleCommand()
    {
    }

    public async Task Run(LPConsoleOptions option, string[] args)
    {
        var info = Program.Container.Resolve<Information>();
        info.Configure(option, true, "relay");

        var control = Program.Container.Resolve<Control>();
        control.Configure();
        await control.LoadAsync();

        if (await this.LoadAsync() == AbortOrContinue.Abort)
        {
            goto Abort;
        }

        if (!control.TryStart())
        {
            goto Abort;
        }

        this.MainLoop(control);

        control.Stop();
        await control.SaveAsync();
        control.Terminate();

Abort:
        Logger.Default.Information("LP Aborted");
        return;
    }

    private async Task<AbortOrContinue> LoadAsync()
    {
        var pri = Program.Container.Resolve<Private>();

        // Load node key.
        if (await this.LoadNodeKey(pri) == AbortOrContinue.Abort)
        {
            return AbortOrContinue.Abort;
        }

        return AbortOrContinue.Continue;
    }

    private async Task<AbortOrContinue> LoadNodeKey(Private pri)
    {
        var file = NodePrivateKey.Filename;
        if (!File.Exists(file))
        {
            return AbortOrContinue.Continue;
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
            Console.Write("Enter password: ");
            var password = Console.ReadLine();
            if (password == null)
            {
                Console.WriteLine();
                return AbortOrContinue.Abort;
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
            pri.NodePrivateKey = key;
            Logger.Default.Information($"Loaded: {file}");
        }
        catch
        {
        }

        return AbortOrContinue.Continue;
    }

    private void MainLoop(Control control)
    {
        while (!control.Core.IsTerminated)
        {
            if (Logger.ViewMode)
            {// View mode
                if (this.SafeKeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                    { // To console mode
                        Logger.ViewMode = false;
                        Console.Write("> ");
                    }
                }
            }
            else
            {// Console mode
                var command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command))
                {
                    if (string.Compare(command, "exit", true) == 0)
                    {// Exit
                        return;
                    }
                    else
                    {
                    }
                }

                // To view mode
                Logger.ViewMode = true;
            }

            control.Core.Sleep(100, 100);
        }
    }

    private bool SafeKeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }
}
