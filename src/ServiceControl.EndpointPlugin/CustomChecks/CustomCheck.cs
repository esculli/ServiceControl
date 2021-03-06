﻿namespace ServiceControl.EndpointPlugin.CustomChecks
{
    using System;
    using NServiceBus;
    using Operations.ServiceControlBackend;
    using Plugin.CustomChecks;
    using Plugin.CustomChecks.Messages;

    public abstract class CustomCheck : ICustomCheck
    {
        protected CustomCheck(string id, string category)
        {
            this.category = category;
            this.id = id;
        }

        public string Category
        {
            get { return category; }
        }

        public void ReportPass()
        {
            ReportToBackend(CheckResult.Pass);
        }


        public void ReportFailed(string failureReason)
        {
            ReportToBackend(CheckResult.Failed(failureReason));
        }

        public string Id
        {
            get { return id; }
        }

        void ReportToBackend(CheckResult result)
        {
            Configure.Instance.Builder.Build<ServiceControlBackend>().Send(new ReportCustomCheckResult
            {
                CustomCheckId = id,
                Category = category,
                Result = result,
                ReportedAt = DateTime.UtcNow
            });
        }

        readonly string category;
        readonly string id;
    }
}