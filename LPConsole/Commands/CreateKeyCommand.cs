// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LPConsole;

[SimpleCommand("createkey")]
public class CreateKeyCommand : ISimpleCommandAsync<CreateKeyOptions>
{
    public async Task Run(CreateKeyOptions options, string[] args)
    {
        Console.WriteLine($"Create Key: {options.Type}");

        if (string.IsNullOrEmpty(options.Filename))
        {
            options.Filename = $"{options.Type}.key";
        }

        Console.WriteLine($"Filename: {options.Filename}");

        Console.WriteLine();

        Console.Write("Enter name: ");
        var name = Console.ReadLine();
        if (name == null)
        {
            goto Abort;
        }

        Console.Write("Enter password: ");
        var password = Console.ReadLine();
        if (password == null)
        {
            goto Abort;
        }

        var nodeKey = NodePrivateKey.Create(name);
        var data = TinyhandSerializer.Serialize(nodeKey);
        var encrypted = PasswordEncrypt.Encrypt(data, password);

        try
        {
            await File.WriteAllBytesAsync(options.Filename, encrypted);
        }
        catch
        {
        }

        Console.WriteLine();
        Console.WriteLine($"{options.Filename} was successfully created.");

        return;

Abort:
        Console.WriteLine();
        Console.WriteLine("aborted.");
    }
}

public record CreateKeyOptions
{
    [SimpleOption("type", description: "Key type (node)")]
    public KeyType Type { get; init; } = KeyType.Node;

    // [SimpleOption("password", description: "Password", Required = true)]
    // public string Password { get; init; } = string.Empty;

    [SimpleOption("file", description: "File name")]
    public string Filename { get; internal set; } = string.Empty;

    public override string ToString() => $"";
}

public enum KeyType
{
    Node,
}
