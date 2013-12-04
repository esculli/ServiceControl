namespace ServiceControl.EndpointPlugin.SagaState
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Sagas;

    public class SagaStateAuditingOverride : PipelineOverride
    {
        public override void Override(BehaviorList<HandlerInvocationContext> behaviorList)
        {
            behaviorList.InsertAfter<SagaPersistenceBehavior, CaptureSagaStateBehavior>();
        }
        public override void Override(BehaviorList<SendPhysicalMessageContext> behaviorList)
        {
            behaviorList.Add<CaptureSagaResultingMessagesBehavior>();
        }
    }
}