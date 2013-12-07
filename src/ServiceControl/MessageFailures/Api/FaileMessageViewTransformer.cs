﻿namespace ServiceControl.MessageFailures.Api
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class FaileMessageViewTransformer : AbstractTransformerCreationTask<FailedMessage>
    {
        public FaileMessageViewTransformer()
        {
            TransformResults = failures => from failure in failures
                select new FailedMessageView
                {
                    MessageId = failure.MessageId,
                    ErrorMessageId = failure.Id,
                    ExceptionMessage = failure.ProcessingAttempts.Last().FailureDetails.Exception.Message,
                    NumberOfProcessingAttempts = failure.ProcessingAttempts.Count(),
                    Status = failure.Status
                };
        }
    }
}