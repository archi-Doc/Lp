// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection.Emit;
using LP.T3CS;
using Tinyhand.IO;

namespace LP;

public static class LPExtentions
{
    public static string To4Hex(this ulong gene) => $"{(ushort)gene:x4}";

    public static string To4Hex(this uint id) => $"{(ushort)id:x4}";
}
