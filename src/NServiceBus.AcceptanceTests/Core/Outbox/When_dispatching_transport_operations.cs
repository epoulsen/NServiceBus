﻿namespace NServiceBus.AcceptanceTests.Core.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_dispatching_transport_operations : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_honor_all_delivery_options()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.DispatchedMessageReceived)
                .Run(TimeSpan.FromSeconds(20));

            Assert.AreEqual(TimeSpan.FromMinutes(1), TimeSpan.Parse(context.HeadersOnDispatchedMessage[Headers.TimeToBeReceived]), "Should honor the TTBR");
            Assert.True(bool.Parse(context.HeadersOnDispatchedMessage[Headers.NonDurableMessage]), "Should honor the durability");
        }

        public class Context : ScenarioContext
        {
            public bool DispatchedMessageReceived { get; set; }
            public IReadOnlyDictionary<string, string> HeadersOnDispatchedMessage { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.EnableOutbox();
                    });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new MessageToDispatch());
                }
            }

            class MessageToDispatchHandler : IHandleMessages<MessageToDispatch>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToDispatch message, IMessageHandlerContext context)
                {
                    TestContext.HeadersOnDispatchedMessage = context.MessageHeaders;
                    TestContext.DispatchedMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        [TimeToBeReceived("00:01:00")]
        [Express]
        public class MessageToDispatch : IMessage
        {
        }
    }
}