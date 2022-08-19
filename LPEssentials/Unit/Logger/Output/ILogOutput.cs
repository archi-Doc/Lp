// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface to receive and output logs.
/// </summary>
public interface ILogOutput
{
    internal delegate void OutputDelegate(LogOutputParameter param);

    public void Output(LogOutputParameter param);
}
