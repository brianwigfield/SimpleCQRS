﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleCqrs.Eventing
{
    public class LocalEventBus : IEventBus
    {
        private readonly IDomainEventHandlerFactory eventHandlerBuilder;
        readonly IEnumerable<Type> eventConverterTypes;
        readonly IDomainEventConverterFactory eventConverterFactory;
        private IDictionary<Type, EventHandlerInvoker> eventHandlerInvokers;
        private readonly IDictionary<Type, Type> eventConverters;

        public LocalEventBus(IEnumerable<Type> eventHandlerTypes, IDomainEventHandlerFactory eventHandlerBuilder, IEnumerable<Type> eventConverterTypes, IDomainEventConverterFactory eventConverterFactory)
        {
            this.eventHandlerBuilder = eventHandlerBuilder;
            this.eventConverterTypes = eventConverterTypes;
            this.eventConverterFactory = eventConverterFactory;
            BuildEventInvokers(eventHandlerTypes);
            eventConverters = GetDomainEventConverters();
        }

        public void PublishEvent(DomainEvent domainEvent)
        {
            while (eventConverters.ContainsKey(domainEvent.GetType()))
                domainEvent = ((dynamic)eventConverterFactory.Create(eventConverters[domainEvent.GetType()])).Convert((dynamic)domainEvent);                

            if(!eventHandlerInvokers.ContainsKey(domainEvent.GetType())) return;

            var eventHandlerInvoker = eventHandlerInvokers[domainEvent.GetType()];
            eventHandlerInvoker.Publish(domainEvent);
        }

        public void PublishEvents(IEnumerable<DomainEvent> domainEvents)
        {
            foreach(var domainEvent in domainEvents)
                PublishEvent(domainEvent);
        }

        private void BuildEventInvokers(IEnumerable<Type> eventHandlerTypes)
        {
            eventHandlerInvokers = new Dictionary<Type, EventHandlerInvoker>();
            foreach(var eventHandlerType in eventHandlerTypes)
            {
                foreach(var domainEventType in GetDomainEventTypes(eventHandlerType))
                {
                    EventHandlerInvoker eventInvoker;
                    if(!eventHandlerInvokers.TryGetValue(domainEventType, out eventInvoker))
                        eventInvoker = new EventHandlerInvoker(eventHandlerBuilder, domainEventType);

                    eventInvoker.AddEventHandlerType(eventHandlerType);
                    eventHandlerInvokers[domainEventType] = eventInvoker;
                }
            }
        }

        private static IEnumerable<Type> GetDomainEventTypes(Type eventHandlerType)
        {
            return from interfaceType in eventHandlerType.GetInterfaces()
                   where interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IHandleDomainEvents<>)
                   select interfaceType.GetGenericArguments()[0];
        }

        private IDictionary<Type, Type> GetDomainEventConverters()
        {
            return eventConverterTypes
                .SelectMany(type => 
                    type.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEventConverter<,>))
                        .Select(i => new KeyValuePair<Type, Type>(i.GetGenericArguments()[0], type)))
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private class EventHandlerInvoker
        {
            private readonly IDomainEventHandlerFactory eventHandlerFactory;
            private readonly Type domainEventType;
            private readonly List<Type> eventHandlerTypes;

            public EventHandlerInvoker(IDomainEventHandlerFactory eventHandlerFactory, Type domainEventType)
            {
                this.eventHandlerFactory = eventHandlerFactory;
                this.domainEventType = domainEventType;
                eventHandlerTypes = new List<Type>();
            }

            public void AddEventHandlerType(Type eventHandlerType)
            {
                eventHandlerTypes.Add(eventHandlerType);
            }

            public void Publish(DomainEvent domainEvent)
            {
                var handleMethod = typeof(IHandleDomainEvents<>).MakeGenericType(domainEventType).GetMethod("Handle");
                foreach(var eventHandlerType in eventHandlerTypes)
                {
                    var eventHandler = eventHandlerFactory.Create(eventHandlerType);
                    handleMethod.Invoke(eventHandler, new object[] {domainEvent});
                }
            }
        }
    }
}