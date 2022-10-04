// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using System.Threading;
using Netsphere;

namespace Netsphere.Ntp;

public partial class NtpCorrection : UnitBase, IUnitSerializable
{
    private const string FileName = "NtpNode.tinyhand";
    private const int ParallelNumber = 3;
    private const int MaxRoundtripMilliseconds = 1000;
    private readonly string[] hostNames =
    {
        "pool.ntp.org",
        "time.google.com",
        "time.facebook.com",
        "time.windows.com",
        "time.apple.com",
        "ntp.nict.jp",
        "time-a-g.nist.gov",
    };

    [TinyhandObject]
    [ValueLinkObject]
    private partial class Item
    {
        public Item()
        {
        }

        public Item(string hostname)
        {
            this.hostname = hostname;
        }

        [Link(Type = ChainType.Ordered, Primary = true)]
        [Key(0)]
        private string hostname = string.Empty;

        [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
        [Key(1)]
        private int roundtripMilliseconds = MaxRoundtripMilliseconds;

        [Link(Type = ChainType.Ordered, Accessibility = ValueLinkAccessibility.Public)]
        [Key(2)]
        private int timeoffsetMilliseconds = 0;
    }

    public NtpCorrection(UnitContext context, ILogger<NtpCorrection> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public async Task CorrectAsync(CancellationToken cancellationToken)
    {
        string[] hostnames;
        lock (this.syncObject)
        {
            hostnames = this.goshujin.HostnameChain.Select(x => x.HostnameValue).ToArray();
        }

        foreach (var x in hostnames)
        {
            using (var client = new UdpClient())
            {
                try
                {
                    client.Connect(x, 123);
                    var packet = NtpPacket.CreateSendPacket();
                    await client.SendAsync(packet.PacketData, cancellationToken);
                    var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
                    packet = new NtpPacket(result.Buffer);

                    this.logger?.TryGet()?.Log($"{x}, RoundtripTime: {packet.RoundtripTime.Milliseconds.ToString()} ms, TimeOffset: {packet.TimeOffset.Milliseconds.ToString()} ms");

                    lock (this.syncObject)
                    {
                        var item = this.goshujin.HostnameChain.FindFirst(x);
                        if (item != null)
                        {
                            item.TimeoffsetMillisecondsValue = packet.TimeOffset.Milliseconds;
                            item.RoundtripMillisecondsValue = packet.RoundtripTime.Milliseconds;
                        }
                    }
                }
                catch
                {
                    this.logger?.TryGet()?.Log($"{x} failed");
                }
            }
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

        lock (this.syncObject)
        {
            foreach (var x in this.hostNames)
            {
                if (!this.goshujin.HostnameChain.ContainsKey(x))
                {
                    this.goshujin.Add(new Item(x));
                }
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

    private object syncObject = new();
    private Item.GoshujinClass goshujin = new();
    private ILogger<NtpCorrection>? logger;
}
