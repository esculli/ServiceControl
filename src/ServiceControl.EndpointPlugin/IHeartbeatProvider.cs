﻿namespace ServiceControl.Plugin.Heartbeats
{
    using Messages;

    //we need this for now until we can patch builder.BuildAll to support abstract base classes
    public interface IHeartbeatInfoProvider
    {
        void HeartbeatExecuted(EndpointHeartbeat heartbeat);
    }
}
