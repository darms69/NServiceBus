﻿namespace NServiceBus.AcceptanceTests.Performance.Receive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class Receive_performance : NServiceBusPerformanceTest
    {
        static int NumberOfTestMessages = 10000;

        [Test]
        public void With_dtc_enabled()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(SendTestMessages)
                    .Done(c => c.Complete)
                    .Repeat(r => r.For<AllTransports>())
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_dtc_suppressed()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.SuppressDistributedTransactions = true));
                            SendTestMessages(b);
                        })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_no_transactions()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Disable());
                        SendTestMessages(b);
                    })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_ambient_tx_suppressed()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DoNotWrapHandlersExecutionInATransactionScope = true));
                        SendTestMessages(b);
                    })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }


        public class Context : PerformanceTestContext
        {
          

            public bool Complete { get; set; }
        }

        public class ReceiveEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 10);
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                static int numberOfMessagesProcessed;


                public void Handle(MyMessage messageThatIsEnlisted)
                {
                    var current = Interlocked.Increment(ref numberOfMessagesProcessed);

                    if (current == 1)
                    {
                        Context.FirstMessageProcessedAt = DateTime.UtcNow;
                    }

                    if (current == Context.NumberOfTestMessages)
                    {
                        Context.LastMessageProcessedAt = DateTime.UtcNow;
                        Context.Complete = true;
                    }

                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }



        protected static void SendTestMessages(EndpointBehaviorBuilder<Context> b)
        {
            b.Given((bus, context) => Parallel.For(0, context.NumberOfTestMessages, (s, c) => bus.SendLocal(new MyMessage())));
        }
    }
}