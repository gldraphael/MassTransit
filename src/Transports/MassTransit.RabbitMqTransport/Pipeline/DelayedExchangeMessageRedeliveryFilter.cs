﻿namespace MassTransit.RabbitMqTransport.Pipeline
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Contexts;
    using GreenPipes;


    /// <summary>
    /// Uses a delayed exchange in RabbitMQ to delay a message retry
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class DelayedExchangeMessageRedeliveryFilter<TMessage> :
        IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        void IProbeSite.Probe(ProbeContext context)
        {
        }

        [DebuggerNonUserCode]
        Task IFilter<ConsumeContext<TMessage>>.Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            context.GetOrAddPayload<MessageRedeliveryContext>(() => new DelayedExchangeMessageRedeliveryContext<TMessage>(context));

            return next.Send(context);
        }
    }
}
