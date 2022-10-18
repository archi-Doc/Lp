// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using Tinyhand;

namespace LPRunner;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record RunnerInformation
{
    public const string Path = "RunnerInformation.tinyhand";

    public static RunnerInformation Create()
        => new RunnerInformation() with
        {
            Image = "archidoc422/lpconsole",
            Tag = "latest",
            Directory = "lp",
            TargetPort = 49152,
            Arguments = "-rootdir \"/lp\" -ns [-port 49152 -test true -alternative false]",
            PublicKeyHex = PrivateKey.Create().ToPublicKey().ToString(),
        };

    public string Image { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public string Directory { get; set; } = string.Empty;

    public int TargetPort { get; set; }

    public string Arguments { get; set; } = string.Empty;

    public string PublicKeyHex { get; set; } = string.Empty;

    [IgnoreMember]
    internal PublicKey PublicKey => new PublicKey(this.PublicKeyHex);
}
