// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Unit;

/*public class UnitBuilderContext
{
    public string UnitName { get; set; } = string.Empty;

    public string RootDirectory { get; set; } = string.Empty;
}*/

public record UnitBuilderContext(string UnitName, string RootDirectory);
