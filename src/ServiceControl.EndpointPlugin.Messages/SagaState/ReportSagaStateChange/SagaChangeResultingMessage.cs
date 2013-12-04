namespace ServiceControl.EndpointPlugin.Messages.SagaState
{
    using System;

    public class SagaChangeResultingMessage
    {
        public string MessageType { get; set; }
        public TimeSpan? RequestedTimeout { get; set; }
        public string TimeSent { get; set; }
        public DateTime? DeliveryDelay { get; set; }
        public string Destination { get; set; }
        public string ResultingMessageId { get; set; }
    }
}