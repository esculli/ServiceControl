namespace ServiceControl.EndpointPlugin.SagaState
{
    using System;
    using System.Linq;
    using Messages.SagaState;
    using NServiceBus;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using Operations.ServiceControlBackend;

    public class CaptureSagaResultingMessagesBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public ServiceControlBackend ServiceControlBackend { get; set; }
        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            AppendMessageToState(context);
            next();
        }

        void AppendMessageToState(SendPhysicalMessageContext context)
        {
            ReportSagaStateChange change;
            if (!context.TryGet(out change))
            {
                return;
            }
            var messages = context.LogicalMessages.ToList();
            if (messages.Count > 1)
            {
                throw new Exception("The SagaAuditing plugin does not support batch messages.");
            }
            if (messages.Count == 0)
            {
                //this can happen on control messages
                //TODO: perhaps audit them??
                return;
            }
            var logicalMessage = messages.First();

            var sagaResultingMessage = new SagaChangeResultingMessage
                {
                    ResultingMessageId = context.MessageToSend.Id,
                    TimeSent = context.MessageToSend.Headers[Headers.TimeSent],
                    MessageType = logicalMessage.MessageType.ToString(),
                    RequestedTimeout = context.SendOptions.DelayDeliveryWith,
                    DeliveryDelay = context.SendOptions.DeliverAt,
                    Destination = context.SendOptions.Destination.ToString()
                };
            change.ResultingMessages.Add(sagaResultingMessage);
            ServiceControlBackend.Send(change);
        }
    }
}