// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

public readonly struct PathInformation
{
    public PathInformation(string path, long length)
    {
        this.Path = path;
        this.Length = length;
    }

    public readonly string Path;
    public readonly long Length;

    public bool IsFile => this.Length >= 0;

    public bool IsDirectory => this.Length < 0;

    public override string ToString()
        => $"{this.Path} ({this.Length})";
}
