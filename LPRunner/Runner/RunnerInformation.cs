// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LPRunner;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record RunnerInformation
{
    public const string Path = "RunnerInformation.tinyhand";

    public static RunnerInformation Create()
        => new RunnerInformation() with
        {
            Directory = "lp",
            TargetPort = 49152,
            RunCommand = "docker run -it --mount type=bind,source=$(pwd)/lp,destination=/lp --rm -p 49152:49152/udp archidoc422/lpconsole -rootdir \"/lp\" -ns [-port 49152 -test true -alternative false]",
        };

    public string Directory { get; set; } = string.Empty;

    public int TargetPort { get; set; }

    public string RunCommand { get; set; } = string.Empty;
}
