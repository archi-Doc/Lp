// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ZenIOBenchmark
{
    public const string DirectoryPath = "test\\folder";

    public ConcurrentDictionary<string, bool> CreatedDirectories { get; } = new();

    public ZenIOBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public DirectoryInfo CreateDirectory()
    {
        return Directory.CreateDirectory(DirectoryPath);
    }

    [Benchmark]
    public bool CreateDirectoryCache()
    {
        return this.CreatedDirectories.GetOrAdd(DirectoryPath, a =>
        {
            Directory.CreateDirectory(DirectoryPath);
            return true;
        });
    }
}
