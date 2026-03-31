// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using CrossChannel;
using Lp.Net;

namespace Lp.T3cs;

public class DomainRadiant : IClockHandTarget
{
    private readonly DomainControl domainControl;
    private readonly CircularQueue<Message> queue = new(LpParameters.DomainRadiantQueueCapacity);

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

    public DomainRadiant(DomainControl domainControl, IChannel<IClockHandTarget> channel)
    {
        this.domainControl = domainControl;

        channel.Open(this);
    }

    public bool TryEnqueue(Message message)
        => this.queue.TryEnqueue(message);

    void IClockHandTarget.OnEverySecond()
    {
        while (this.queue.TryDequeue(out var message))
        {
            var domainData = this.domainControl.GetDomainData(message.DomainHash);
            if (domainData is not null)
            {
                var channel = new ResponseChannel<int>();
                domainData.RadiateProof(message.Proof, ref channel);
                // domainData.SetConvergentCount(message.Destination, convergentCount);
            }
        }
    }

    void IClockHandTarget.OnEveryMinute()
    {
    }
}
