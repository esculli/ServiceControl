namespace ServiceControl.Operations.SagaState
{
    using System.Linq;
    using EndpointPlugin.Messages.SagaState;
    using Infrastructure;
    using NServiceBus;
    using Raven.Client;

    public class SagaAuditHandler : IHandleMessages<ReportSagaStateChange>
    {
        public IDocumentStore Store { get; set; }
        public IBus Bus { get; set; }

        public void Handle(ReportSagaStateChange message)
        {
            using (var session = Store.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var id = DeterministicGuid.MakeId(message.Endpoint, message.SagaId.ToString());
                var sagaHistory = session.Load<SagaHistory>(id);

                if (sagaHistory == null)
                {
                    sagaHistory = new SagaHistory
                        {
                            Id = id,
                            SagaId = message.SagaId,
                        };
                }

                var sagaStateChange = sagaHistory.Changes.FirstOrDefault(x => x.InitiatingMessage.InitiatingMessageId == message.InitiatingMessage.InitiatingMessageId);
                if (sagaStateChange == null)
                {
                    sagaStateChange = new SagaStateChange();
                    sagaHistory.Changes.Add(sagaStateChange);
                }

                sagaStateChange.Timestamp = message.ChangeTimestamp;
                sagaStateChange.StateAfterChange = message.SagaState;
                sagaStateChange.Endpoint = message.Endpoint;
                sagaStateChange.IsNew = message.IsNew;

                if (sagaStateChange.InitiatingMessage == null)
                {
                    sagaStateChange.InitiatingMessage = new InitiatingMessage
                        {
                            InitiatingMessageId = message.InitiatingMessage.InitiatingMessageId,
                            IsSagaTimeoutMessage = message.InitiatingMessage.IsSagaTimeoutMessage,
                            OriginatingEndpoint = message.InitiatingMessage.OriginatingEndpoint,
                            OriginatingMachine = message.InitiatingMessage.OriginatingMachine,
                            ProcessingEnded = message.InitiatingMessage.ProcessingEnded,
                            ProcessingStarted = message.InitiatingMessage.ProcessingStarted,
                            TimeSent = message.InitiatingMessage.TimeSent,
                            MessageType = message.InitiatingMessage.MessageType,
                        };
                }

                foreach (var toAdd in message.ResultingMessages)
                {
                    var resultingMessage = sagaStateChange.OutgoingMessages.FirstOrDefault(x => x.ResultingMessageId == toAdd.ResultingMessageId);
                    if (resultingMessage == null)
                    {
                        resultingMessage = new ResultingMessage();
                        sagaStateChange.OutgoingMessages.Add(resultingMessage);
                    }
                    resultingMessage.MessageType = toAdd.MessageType;
                    resultingMessage.ResultingMessageId = toAdd.ResultingMessageId;
                    resultingMessage.TimeSent = toAdd.TimeSent;
                    resultingMessage.DeliveryDelay = toAdd.DeliveryDelay;
                    resultingMessage.Destination = toAdd.Destination;
                }

                session.Store(sagaHistory);
                session.SaveChanges();
            }
        }
    }
}