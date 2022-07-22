// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogOutput
{
    internal delegate void OutputDelegate(LogOutputParameter param);

    public void Output(LogOutputParameter param);

    /*
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public readonly record struct Parameter(Type LogSourceType, LogLevel LogLevel, int EventId);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    */
}
