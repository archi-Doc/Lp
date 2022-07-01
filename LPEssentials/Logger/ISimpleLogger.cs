// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public interface ISimpleLogger
{
    public void Debug(string message);

    public void Information(string message);

    public void Warning(string message);

    public void Error(string message);

    public void Fatal(string message);

    public void Debug(ulong hash) => this.Debug(HashedString.Get(hash));

    public void Information(ulong hash) => this.Information(HashedString.Get(hash));

    public void Warning(ulong hash) => this.Warning(HashedString.Get(hash));

    public void Error(ulong hash) => this.Error(HashedString.Get(hash));

    public void Fatal(ulong hash) => this.Fatal(HashedString.Get(hash));

    public bool FatalFlag { get; }
}
