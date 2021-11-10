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

namespace LPConsole
{
    [SimpleCommand("createkey")]
    public class CreateKeyCommand : ISimpleCommandAsync<CreateKeyOptions>
    {
        public async Task Run(CreateKeyOptions options, string[] args)
        {
            Console.WriteLine($"Create Key: {options.Type}");
            Console.WriteLine();

            Console.Write("Enter name: ");
            var name = Console.ReadLine();

            Console.Write("Enter password: ");
            var password = Console.ReadLine() ?? string.Empty;

            var nodeKey = NodePrivateKey.Create(name);
            var data = TinyhandSerializer.Serialize(nodeKey);
            var encrypted = PasswordEncrypt.Encrypt(data, password);

            await File.WriteAllBytesAsync(options.Filename, encrypted);
        }
    }

    public record CreateKeyOptions
    {
        [SimpleOption("type", description: "Key type (node)")]
        public string Type { get; init; } = string.Empty;

        // [SimpleOption("password", description: "Password", Required = true)]
        // public string Password { get; init; } = string.Empty;

        [SimpleOption("file", description: "File name")]
        public string Filename { get; init; } = string.Empty;

        public override string ToString() => $"";
    }
}
