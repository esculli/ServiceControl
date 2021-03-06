﻿namespace ServiceBus.Management.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using MessageAuditing;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using ServiceControl.EventLog;

    public class When_a_message_has_failed : AcceptanceTest
    {
        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServerWithoutAudit>()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServerWithoutAudit>()
                    .AuditTo(Address.Parse("audit"));
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyContext Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.EndpointNameOfReceivingEndpoint = Configure.EndpointName;
                    Context.MessageId = Bus.CurrentMessageContext.Id.Replace(@"\", "-");
                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        public class MyContext : ScenarioContext
        {
            public string MessageId { get; set; }
            public Message Message { get; set; }
            public List<EventLogItem> LogEntries { get; set; }
            public string EndpointNameOfReceivingEndpoint { get; set; }
        }

        bool IsErrorMessageStored(MyContext context, MyContext c)
        {
            lock (context)
            {
                if (c.Message != null)
                {
                    return true;
                }

                if (c.MessageId == null)
                {
                    return false;
                }

                var message =
                    Get<Message>("/api/messages/" + context.MessageId + "-" + context.EndpointNameOfReceivingEndpoint);

                if (message == null)
                {
                    return false;
                }

                c.Message = message;

                return true;
            }
        }

        [Test]
        public void Should_be_imported_and_accessible_via_the_rest_api()
        {
            var context = new MyContext();

            Scenario.Define(context)
                .WithEndpoint<ManagementEndpoint>(c => c.AppConfig(PathToAppConfig))
                .WithEndpoint<Sender>(b => b.Given(bus => bus.Send(new MyMessage())))
                .WithEndpoint<Receiver>()
                .Done(c => IsErrorMessageStored(context, c))
                .Run();

            // The message Ids may contain a \ if they are from older versions. 
            Assert.AreEqual(context.MessageId, context.Message.MessageId.Replace(@"\","-"),
                "The returned message should match the processed one");
            Assert.AreEqual(MessageStatus.Failed, context.Message.Status, "Status should be set to failed");
            Assert.AreEqual(1, context.Message.FailureDetails.NumberOfTimesFailed, "Failed count should be 1");
            Assert.AreEqual("Simulated exception", context.Message.FailureDetails.Exception.Message,
                "Exception message should be captured");

        }

        [Test]
        public void Should_add_an_event_log_item()
        {
            var context = new MyContext();

            Scenario.Define(context)
                .WithEndpoint<ManagementEndpoint>(c => c.AppConfig(PathToAppConfig))
                .WithEndpoint<Sender>(b => b.Given(bus => bus.Send(new MyMessage())))
                .WithEndpoint<Receiver>()
                .Done(IsEventLogDataAvailable)
                .Run();

            Assert.AreEqual(1, context.LogEntries.Count);
            Assert.IsTrue(context.LogEntries[0].Description.Contains("exception"), "For failed messages, the description should contain the exception information");
            var containsFailedMessageId = context.LogEntries[0].RelatedTo.Any(item => item.Contains("/failedMessageId/"));
            Assert.IsTrue(containsFailedMessageId, "For failed message, the RelatedId must contain the api url to retrieve additional details about the failed message");
        }


        bool IsEventLogDataAvailable(MyContext c)
        {
            var logEntries = Get<EventLogItem[]>("/api/eventlogitems/");
            if (logEntries == null || logEntries.Length == 0)
            {
                System.Threading.Thread.Sleep(5000);
                return false;
            }
            c.LogEntries = logEntries.ToList();
            return true;
        }
    }
}