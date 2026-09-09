// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices.Remote;
using Xunit;

namespace xUnitTest;

public class OrderedLineWriterTest
{
    [Fact]
    public void CapacityIsRoundedUpToAPowerOfTwo()
    {
        Assert.Equal(8, new OrderedLineWriter(5, (_, _) => { }).Capacity);
        Assert.Equal(16, new OrderedLineWriter(16, (_, _) => { }).Capacity);
    }

    [Fact]
    public void LinesArrivingInOrderArePassedThroughImmediately()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(8, recorder.Write);
        for (var i = 0; i < 5; i++)
        {
            writer.Add(i, i.ToString(), ConsoleColor.Gray);
        }

        Assert.Equal(["0", "1", "2", "3", "4"], recorder.Messages);
        Assert.Equal(5, writer.NextLine);
    }

    [Fact]
    public void OutOfOrderLinesAreBufferedAndReleasedWhenTheGapIsFilled()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(8, recorder.Write);

        writer.Add(2, "c", ConsoleColor.Gray);
        writer.Add(1, "b", ConsoleColor.Gray);
        Assert.Empty(recorder.Messages);

        writer.Add(0, "a", ConsoleColor.Gray);
        Assert.Equal(["a", "b", "c"], recorder.Messages);
        Assert.Equal(3, writer.NextLine);
    }

    [Fact]
    public void ColorsFollowTheirLineThroughTheReorderBuffer()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(4, recorder.Write);

        writer.Add(1, "b", ConsoleColor.Red);
        writer.Add(0, "a", ConsoleColor.Blue);

        Assert.Equal([ConsoleColor.Blue, ConsoleColor.Red], recorder.Colors);
    }

    [Fact]
    public void ALineOlderThanTheExpectedOneIsWrittenRightAway()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(4, recorder.Write);

        writer.Add(0, "a", ConsoleColor.Gray);
        writer.Add(0, "duplicate", ConsoleColor.Gray);

        Assert.Equal(["a", "duplicate"], recorder.Messages);
        Assert.Equal(1, writer.NextLine);
    }

    [Fact]
    public void ADuplicateOfABufferedLineIsDropped()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(4, recorder.Write);

        writer.Add(1, "b", ConsoleColor.Gray);
        writer.Add(1, "b-again", ConsoleColor.Gray);
        writer.Add(0, "a", ConsoleColor.Gray);

        Assert.Equal(["a", "b"], recorder.Messages);
    }

    [Fact]
    public void ALineFarAheadFlushesTheBufferInsteadOfStallingForever()
    {
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(4, recorder.Write);

        writer.Add(1, "b", ConsoleColor.Gray);
        writer.Add(3, "d", ConsoleColor.Gray);
        writer.Add(1000, "far", ConsoleColor.Gray);

        Assert.Equal(["b", "d", "far"], recorder.Messages);
        Assert.Equal(1001, writer.NextLine);
    }

    [Fact]
    public void EveryLineIsWrittenExactlyOnceWhenArrivalIsShuffled()
    {
        const int count = 500;
        var recorder = new Recorder();
        var writer = new OrderedLineWriter(64, recorder.Write);

        var order = Enumerable.Range(0, count).ToArray();
        var random = new Random(42);
        for (var i = order.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (var line in order)
        {
            writer.Add(line, line.ToString(), ConsoleColor.Gray);
        }

        Assert.Equal(count, recorder.Messages.Count);
        Assert.Equal(Enumerable.Range(0, count).Select(x => x.ToString()).Order(), recorder.Messages.Order());
    }

    private sealed class Recorder
    {
        public List<string?> Messages { get; } = new();

        public List<ConsoleColor> Colors { get; } = new();

        public void Write(string? message, ConsoleColor color)
        {
            this.Messages.Add(message);
            this.Colors.Add(color);
        }
    }
}
