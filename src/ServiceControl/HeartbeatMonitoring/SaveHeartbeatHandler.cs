﻿namespace ServiceControl.HeartbeatMonitoring
{
    using Infrastructure;
    using NServiceBus;
    using Plugin.Heartbeats.Messages;
    using Raven.Client;
    using ServiceBus.Management.MessageAuditing;

    public class SaveHeartbeatHandler : IHandleMessages<EndpointHeartbeat>
    {
        public IDocumentStore Store { get; set; }
        public IBus Bus { get; set; }

        public void Handle(EndpointHeartbeat message)
        {
            using (var session = Store.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var originatingEndpoint = EndpointDetails.OriginatingEndpoint(Bus.CurrentMessageContext.Headers);
                var id = DeterministicGuid.MakeId(originatingEndpoint.Name, originatingEndpoint.Machine);
                var heartbeat = session.Load<Heartbeat>(id) ?? new Heartbeat
                {
                    Id = id,
                    ReportedStatus = Status.New,
                };

                if (message.ExecutedAt > heartbeat.LastReportAt)
                {
                    return;
                }

                heartbeat.LastReportAt = message.ExecutedAt;
                heartbeat.OriginatingEndpoint = originatingEndpoint;

                session.Store(heartbeat);
                session.SaveChanges();
            }
        }
    }
}