namespace ServiceControl.EndpointPlugin.Messages.SagaState
{
    using System;
    using System.Collections.Generic;

    public class ReportSagaStateChange
    {
        public ReportSagaStateChange()
        {
            ResultingMessages = new List<SagaChangeResultingMessage>();
        }

        public string SagaState { get; set; }
        public Guid SagaId { get; set; }
        public DateTimeOffset ChangeTimestamp { get; set; }
        public SagaChangeInitiatingMessage InitiatingMessage { get; set; }
        public List<SagaChangeResultingMessage> ResultingMessages { get; set; }
        public string Endpoint { get; set; }
        public bool IsNew { get; set; }
    }
}
