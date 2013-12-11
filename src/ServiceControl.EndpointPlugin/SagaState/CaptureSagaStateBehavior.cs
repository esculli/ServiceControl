namespace ServiceControl.EndpointPlugin.SagaState
{
    using System;
    using Messages.SagaState;
    using NServiceBus;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;

    class CaptureSagaStateBehavior : IBehavior<HandlerInvocationContext>
    {
        public void Invoke(HandlerInvocationContext context, Action next)
        {
            var saga = context.MessageHandler.Instance as ISaga;
            if (saga != null)
            {
                AuditSaga(saga, context);
            }

            next();
        }

        void AuditSaga(ISaga saga, HandlerInvocationContext context)
        {
            var activeSagaInstance = context.Get<ActiveSagaInstance>();
            var sagaStateString = Serializer.Serialize(saga.Entity);
            var sagaAudit = new ReportSagaStateChange
                {
                    SagaState = sagaStateString,
                    ChangeTimestamp = DateTimeOffset.UtcNow,
                    SagaId = saga.Entity.Id,
                    IsNew = activeSagaInstance.IsNew,
                    InitiatingMessage = new SagaChangeInitiatingMessage
                    {
                        //TODO: what happens when we have an in memory send and no physical messgae?
                            InitiatingMessageId = context.PhysicalMessage.Id,
                            OriginatingMachine = context.LogicalMessage.Headers[Headers.OriginatingMachine],
                            OriginatingEndpoint = context.LogicalMessage.Headers[Headers.OriginatingEndpoint],
                            MessageType = context.LogicalMessage.GetType().Name,
                            TimeSent = context.LogicalMessage.Headers[Headers.TimeSent],
                            ProcessingStarted = context.LogicalMessage.Headers[Headers.ProcessingStarted],
                            ProcessingEnded = context.LogicalMessage.Headers[Headers.ProcessingEnded],
                        },
                  
                };
            context.Set(sagaAudit);
        }

    }
}