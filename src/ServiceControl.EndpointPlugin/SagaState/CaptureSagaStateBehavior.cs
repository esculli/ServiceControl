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
            var transportMessage = context.PhysicalMessage;
            var sagaStateString = Serializer.Serialize(saga.Entity);
            var sagaAudit = new ReportSagaStateChange
                {
                    SagaState = sagaStateString,
                    ChangeTimestamp = DateTimeOffset.UtcNow,
                    SagaId = saga.Entity.Id,
                    IsNew = activeSagaInstance.IsNew,
                    InitiatingMessage = new SagaChangeInitiatingMessage
                        {
                            InitiatingMessageId = transportMessage.Id,
                            OriginatingMachine = context.PhysicalMessage.Headers[Headers.OriginatingMachine],
                            OriginatingEndpoint = context.PhysicalMessage.Headers[Headers.OriginatingEndpoint],
                            MessageType = context.LogicalMessage.GetType().Name,
                            TimeSent = context.PhysicalMessage.Headers[Headers.TimeSent],
                            ProcessingStarted = context.PhysicalMessage.Headers[Headers.ProcessingStarted],
                            ProcessingEnded = context.PhysicalMessage.Headers[Headers.ProcessingEnded],
                        },
                  
                };
            context.Set(sagaAudit);
        }

    }
}