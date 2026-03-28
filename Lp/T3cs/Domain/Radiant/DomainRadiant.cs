// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using CrossChannel;
using Lp.Net;

namespace Lp.T3cs;

public class DomainRadiant : IClockHandTarget
{
    public const int QueueCapacity = 32;

    private static readonly DomainControl domainControl;
    private readonly CircularQueue<Message> queue = new(QueueCapacity);

    public record class Message(ClientConnection Destination, ulong DomainHash, CertificateProof Proof);

    /*public readonly record struct Message
    {
        // public readonly ClientConnection Source;

        public readonly ClientConnection Destination;

        public readonly ulong DomainHash;

        public readonly CertificateProof Proof;

        public Message(ClientConnection destination, ulong domainHash, CertificateProof proof)
        {
            // this.Source = source;
            this.Destination = destination;
            this.DomainHash = domainHash;
            this.Proof = proof;
        }
    }*/

    private static async Task ProcessMessage(Message message)
    {
        var domainService = message.Destination.GetService<IDomainService>();
        var convergentCount = await domainService.Radiate(message.DomainHash, message.Proof).ConfigureAwait(false);

        var domainData = domainControl.GetDomainData(message.DomainHash);
        if (domainData is not null)
        {
            // domainData.SetConvergentCount(message.Destination, convergentCount);
        }
    }

    public DomainRadiant(IChannel<IClockHandTarget> channel)
    {
        channel.Open(this);
    }

    public bool TryEnqueue(Message message)
        => this.queue.TryEnqueue(message);

    void IClockHandTarget.OnEverySecond()
    {
        while (this.queue.TryDequeue(out var message))
        {
            _ = ProcessMessage(message);
        }
    }

    void IClockHandTarget.OnEveryMinute()
    {
    }
}
