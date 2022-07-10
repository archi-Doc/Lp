// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Arc.Crypto;
using LP;

namespace Sandbox;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Sandbox");

        Test();
    }

    public static void Test()
    {
        var pass = "pass";
        var encrypted = PasswordEncrypt.Encrypt(new byte[] { 1, 2, }, pass);
        var item = new KeyVaultItem("test.key", PasswordEncrypt.GetPasswordHint(pass), encrypted);
        var array = new KeyVaultItem[] { item, item, };

        var t = Tinyhand.TinyhandSerializer.SerializeToString(array, Tinyhand.TinyhandSerializerOptions.Standard.WithCompose(Tinyhand.TinyhandComposeOption.Standard));
    }
}
