// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using Netsphere;

namespace LP.Machines.Ntp;

public class NtpPacket
{
    private const long CompensatingRate32 = 0x100000000L;
    private const double CompensatingRate16 = 0x10000d;

    private static readonly DateTime CompensatingDateTime = new DateTime(1900, 1, 1);
    private static readonly DateTime PassedCompensatingDateTime = CompensatingDateTime.AddSeconds(uint.MaxValue);

    public byte[] PacketData { get; private set; }

    public DateTime NtpPacketCreatedTime { get; private set; }

    public static NtpPacket CreateSendPacket()
    {
        var packet = new byte[48];
        packet[0] = 0x1B;
        FillTransmitTimestamp(packet);
        return new NtpPacket(packet);
    }

    public TimeSpan DifferentTimeSpan
    {
        get
        {
            long offsetTick = ((this.ReceiveTimestamp - this.OriginateTimestamp) + (this.TransmitTimestamp - this.NtpPacketCreatedTime)).Ticks / 2;
            return new TimeSpan(offsetTick);
        }
    }

    public TimeSpan NetworkDelay
    {
        get { return (this.NtpPacketCreatedTime - this.OriginateTimestamp) + (this.TransmitTimestamp - this.ReceiveTimestamp); }
    }

    public NtpPacket(byte[] packetData)
    {
        this.PacketData = packetData;
        this.NtpPacketCreatedTime = DateTime.Now;
    }

    private static DateTime GetCompensatingDatetime(uint seconds)
        => (seconds & 0x80000000) == 0 ? PassedCompensatingDateTime : CompensatingDateTime;

    private static DateTime GetCompensatingDatetime(DateTime dateTime)
        => dateTime >= PassedCompensatingDateTime ? PassedCompensatingDateTime : CompensatingDateTime;

    private static void FillTransmitTimestamp(byte[] ntpPacket)
    {
        byte[] time = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(DateTimeToNtpTimeStamp(DateTime.UtcNow)));
        Array.Copy(time, 0, ntpPacket, 40, 8);
    }

    private static double SignedFixedPointToDouble(int signedFixedPoint)
    {
        short number = (short)(signedFixedPoint >> 16);
        ushort fraction = (ushort)(signedFixedPoint & short.MaxValue);

        return number + ((double)fraction / CompensatingRate16);
    }

    private static DateTime NtpTimeStampToDateTime(long ntpTimeStamp)
    {
        uint seconds = (uint)(ntpTimeStamp >> 32);
        uint secondsFraction = (uint)(ntpTimeStamp & uint.MaxValue);

        long milliseconds = ((long)seconds * 1000) + (secondsFraction * 1000 / CompensatingRate32);
        return GetCompensatingDatetime(seconds) + TimeSpan.FromMilliseconds(milliseconds);
    }

    private static long DateTimeToNtpTimeStamp(DateTime dateTime)
    {
        DateTime compensatingDatetime = GetCompensatingDatetime(dateTime);
        double ntpStandardTick = (dateTime - compensatingDatetime).TotalMilliseconds;

        uint seconds = (uint)(dateTime - compensatingDatetime).TotalSeconds;
        uint secondsFraction = (uint)((ntpStandardTick % 1000) * CompensatingRate32 / 1000);

        return (long)(((ulong)seconds << 32) | secondsFraction);
    }

    public int LeapIndicator
    {
        get { return this.PacketData[0] >> 6 & 0x03; }
    }

    public int Version
    {
        get { return this.PacketData[0] >> 3 & 0x03; }
    }

    public int Mode
    {
        get { return this.PacketData[0] & 0x03; }
    }

    public int Stratum
    {
        get { return this.PacketData[1]; }
    }

    public int PollInterval
    {
        get
        {
            int interval = (sbyte)this.PacketData[2];
            switch (interval)
            {
                case 0: return 0;
                case 1: return 1;
                default: return (int)Math.Pow(2, interval);
            }
        }
    }

    public double Precision
        => Math.Pow(2, (sbyte)this.PacketData[3]);

    public double RootDelay
    => SignedFixedPointToDouble(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.PacketData, 4)));

    public double RootDispersion
    => SignedFixedPointToDouble(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.PacketData, 8)));

    public DateTime ReferenceTimestamp
        => NtpTimeStampToDateTime(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.PacketData, 16))).ToLocalTime();

    public DateTime OriginateTimestamp
        => NtpTimeStampToDateTime(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.PacketData, 24))).ToLocalTime();

    public DateTime ReceiveTimestamp
        => NtpTimeStampToDateTime(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.PacketData, 32))).ToLocalTime();

    public DateTime TransmitTimestamp
        => NtpTimeStampToDateTime(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.PacketData, 40))).ToLocalTime();
}
