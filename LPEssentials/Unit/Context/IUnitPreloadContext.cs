// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Preload delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPreloadContext
{
    public string UnitName { get; set; }

    public string RootDirectory { get; set; }

    public string DataDirectory { get; set; }

    public UnitArguments Arguments { get; }
}
