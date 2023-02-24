// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Provides a set of methods to treat local file paths and cloud paths in the same way.<br/>
/// </summary>
[TinyhandObject]
public partial class DataPath
{
    public const string HttpsPathHeader = "https://";
    public const string AwsPath = "amazonaws.com";

    public enum Type
    {
        Invalid,
        LocalDirectory,
        LocalFile,
        S3Directory,
        S3File,
    }

    public DataPath()
    {
    }

    public Type SetDirectoryPath(string path)
    {
        var span = path.AsSpan();
        if (span.StartsWith(HttpsPathHeader))
        {// https://aaa.amazonaws.com/bbb
            span = span.Slice(HttpsPathHeader.Length);

            var index = span.IndexOf('/');
            if (index == -1)
            {
                index = span.Length;
            }

            index -= AwsPath.Length;
            if (index < 0)
            {
                index = 0;
            }

            span = span.Slice(index);
            if (span.StartsWith(AwsPath))
            {// amazonaws.com
                if (span[span.Length - 1] == '/')
                {// path
                    this.Path = path;
                }
                else
                {// path + '/'
                    this.Path = path + '/';
                }

                this.PathType = Type.S3Directory;
                return this.PathType;
            }

            return Type.Invalid;
        }

        // Other
        if (span[span.Length - 1] == '/')
        {// path
            this.Path = path;
        }
        else
        {// path + '/'
            this.Path = path + '/';
        }

        this.PathType = Type.LocalDirectory;
        return this.PathType;
    }

    [Key(0)]
    public Type PathType { get; protected set; }

    [Key(1)]
    public string Path { get; protected set; } = string.Empty;
}

public enum DataPathResult
{
    Success,
}
