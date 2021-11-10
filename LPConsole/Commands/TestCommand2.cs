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
    [SimpleCommand("test")]
    public class TestCommand : ISimpleCommandAsync<TestOptions>
    {
        public async Task Run(TestOptions options, string[] args)
        {
            Console.WriteLine($"Create Key: {options.Type}");

            if (string.IsNullOrEmpty(options.Filename))
            {
                options.Filename = "Node.key";
            }

            Console.WriteLine($"Filename: {options.Filename}");

            Console.WriteLine();

            Console.Write("Enter name: ");
            var name = Console.ReadLine();

            Console.Write("Enter password: ");
            var password = Console.ReadLine() ?? string.Empty;

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
        }
    }

    public record TestOptions
    {
        [SimpleOption("type", description: "Key type (node)")]
        public string Type { get; init; } = string.Empty;

        // [SimpleOption("password", description: "Password", Required = true)]
        // public string Password { get; init; } = string.Empty;

        [SimpleOption("file", description: "File name")]
        public string Filename { get; internal set; } = string.Empty;

        public override string ToString() => $"";
    }
}
