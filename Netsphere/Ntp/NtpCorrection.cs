﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Netsphere;

namespace Netsphere.Ntp;

public partial class NtpCorrection : UnitBase, IUnitSerializable
{
    private const string FileName = "NtpNode.tinyhand";
    private const int ParallelNumber = 2;
    private const int MaxRoundtripMilliseconds = 1000;
    private readonly string[] hostNames =
    {
        "pool.ntp.org",
        "time.google.com",
        "time.facebook.com",
        "time.windows.com",
        "ntp.nict.jp",
        "time-a-g.nist.gov",
    };

    [TinyhandObject]
    [ValueLinkObject]
    private partial class Item
    {
        public enum State
        {
            Initial,
            Retrieved,
        }

        [Link(Type = ChainType.List, Name = "List", Primary = true)]
        public Item()
        {
        }

        public Item(string hostname)
        {
            this.hostname = hostname;
        }

        [IgnoreMember]
        public State CurrentState { get; set; } = State.Initial;

        [IgnoreMember]
        public long TimeoffsetMilliseconds { get; set; }

        [Link(Type = ChainType.Ordered)]
        [Key(0)]
        private string hostname = string.Empty;

        [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
        [Key(1)]
        private int roundtripMilliseconds = MaxRoundtripMilliseconds;
    }

    public NtpCorrection(UnitContext context, ILogger<NtpCorrection> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public async Task CorrectAsync(CancellationToken cancellationToken)
    {
Retry:
        string[] hostnames;
        lock (this.syncObject)
        {
            hostnames = this.goshujin.RoundtripMillisecondsChain.Where(x => x.CurrentState == Item.State.Initial).Select(x => x.HostnameValue).Take(ParallelNumber).ToArray();
        }

        if (hostnames.Length == 0)
        {
            return;
        }

        await Parallel.ForEachAsync(hostnames, this.Process);
        if (this.timeoffsetCount == 0)
        {
            this.logger?.TryGet(LogLevel.Error)?.Log("Retry");
            goto Retry;
        }
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        if (this.hostNames.Length == 0)
        {
            return false;
        }

        var hostname = this.hostNames[LP.Random.Pseudo.NextInt32(this.hostNames.Length)];
        using (var client = new UdpClient())
        {
            try
            {
                client.Connect(hostname, 123);
                var packet = NtpPacket.CreateSendPacket();
                await client.SendAsync(packet.PacketData, cancellationToken);
                var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public (long MeanTimeoffset, int TimeoffsetCount) GetTimeoffset()
        => (this.meanTimeoffset, this.timeoffsetCount);

    public bool TryGetCorrectedUtcNow(out DateTime utcNow)
    {
        if (this.timeoffsetCount == 0)
        {
            utcNow = Time.GetFixedUtcNow();
            return false;
        }
        else
        {
            utcNow = Time.GetFixedUtcNow() + TimeSpan.FromMilliseconds(this.meanTimeoffset);
            return true;
        }
    }

    public async Task LoadAsync(UnitMessage.LoadAsync message)
    {
        var path = Path.Combine(message.DataPath, FileName);
        try
        {
            if (File.Exists(path))
            {
                var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
                var goshujin = TinyhandSerializer.DeserializeFromUtf8<Item.GoshujinClass>(bytes);
                if (goshujin != null)
                {
                    this.goshujin = goshujin;
                }
            }
        }
        catch
        {
            this.logger?.TryGet(LogLevel.Error)?.Log($"Could not load '{path}'");
        }

        this.ResetHostnames();
    }

    public void ResetHostnames()
    {
        lock (this.syncObject)
        {
            foreach (var x in this.hostNames)
            {
                if (!this.goshujin.HostnameChain.ContainsKey(x))
                {
                    this.goshujin.Add(new Item(x));
                }
            }

            // Reset host
            foreach (var x in this.goshujin)
            {
                x.CurrentState = Item.State.Initial;
            }
        }
    }

    public async Task SaveAsync(UnitMessage.SaveAsync message)
    {
        var path = Path.Combine(message.DataPath, FileName);
        try
        {
            using (var file = File.Open(path, FileMode.Create))
            {
                byte[] b;
                lock (this.syncObject)
                {
                    b = TinyhandSerializer.SerializeToUtf8(this.goshujin);
                }

                await file.WriteAsync(b);
            }
        }
        catch
        {
        }
    }

    private async ValueTask Process(string hostname, CancellationToken cancellationToken)
    {
        using (var client = new UdpClient())
        {
            try
            {
                client.Connect(hostname, 123);
                var packet = NtpPacket.CreateSendPacket();
                await client.SendAsync(packet.PacketData, cancellationToken);
                var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
                packet = new NtpPacket(result.Buffer);

                this.logger?.TryGet()?.Log($"{hostname}, RoundtripTime: {(int)packet.RoundtripTime.TotalMilliseconds} ms, TimeOffset: {(int)packet.TimeOffset.TotalMilliseconds} ms");

                lock (this.syncObject)
                {
                    var item = this.goshujin.HostnameChain.FindFirst(hostname);
                    if (item != null)
                    {
                        item.CurrentState = Item.State.Retrieved;
                        item.TimeoffsetMilliseconds = (long)packet.TimeOffset.TotalMilliseconds;
                        item.RoundtripMillisecondsValue = (int)packet.RoundtripTime.TotalMilliseconds;
                        this.UpdateTimeoffset();
                    }
                }
            }
            catch
            {
                this.logger?.TryGet(LogLevel.Error)?.Log($"{hostname}");

                lock (this.syncObject)
                {
                    var item = this.goshujin.HostnameChain.FindFirst(hostname);
                    if (item != null)
                    {// Remove item
                        item.Goshujin = null;
                    }
                }
            }
        }
    }

    private void UpdateTimeoffset()
    {// lock (this.syncObject)
        int count = 0;
        long timeoffset = 0;

        foreach (var x in this.goshujin.Where(x => x.CurrentState == Item.State.Retrieved))
        {
            count++;
            timeoffset += x.TimeoffsetMilliseconds;
        }

        this.timeoffsetCount = count;
        if (count != 0)
        {
            this.meanTimeoffset = timeoffset / count;
        }
        else
        {
            this.meanTimeoffset = 0;
        }
    }

    private object syncObject = new();
    private Item.GoshujinClass goshujin = new();
    private int timeoffsetCount;
    private long meanTimeoffset;
    private ILogger<NtpCorrection>? logger;
}