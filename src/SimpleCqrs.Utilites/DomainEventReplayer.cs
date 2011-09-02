using System;
using System.Collections.Generic;
using System.Linq;
using SimpleCqrs.Eventing;

namespace SimpleCqrs.Utilites
{
    public class DomainEventReplayer
    {
        readonly IEventStore _eventStore;
        readonly IEventBus _eventBus;
        readonly ITypeCatalog _typeCatalog;

        public DomainEventReplayer(IEventStore eventStore, IEventBus eventBus, ITypeCatalog typeCatalog)
        {
            _eventStore = eventStore;
            _eventBus = eventBus;
            _typeCatalog = typeCatalog;
        }

        public void ReplayEventsForHandlerType(Type handlerType)
        {
            ReplayEventsForHandlerType(handlerType, DateTime.MinValue, DateTime.MaxValue);
        }

        public void ReplayEventsForHandlerType(Type handlerType, DateTime startDate, DateTime endDate)
        {
            var domainEventTypes = ExpandEventTypesWithAncestors(GetDomainEventTypesHandledByHandler(handlerType));

            var domainEvents = _eventStore.GetEventsByEventTypes(domainEventTypes, startDate, endDate).OrderBy(_ => _.Sequence);
            _eventBus.PublishEvents(domainEvents);
        }

        public IEnumerable<Type> ExpandEventTypesWithAncestors(IEnumerable<Type> eventTypes)
        {
            var ancestors = new List<Type>();
            foreach (var eventType in _typeCatalog.LoadedTypes)
            {
                ancestors.AddRange(eventType.GetInterfaces()
                                       .Where(_ =>
                                              _.IsGenericType &&
                                              _.GetGenericTypeDefinition() == typeof (IEventConverter<,>) &&
                                              eventTypes.Any(e => e == _.GetGenericArguments()[1]))
                                       .Select(_ => _.GetGenericArguments()[0]));

            }
            return eventTypes.Concat(ancestors);
        }

        public IEnumerable<Type> GetDomainEventTypesHandledByHandler(Type handlerType)
        {
            return (from i in handlerType.GetInterfaces()
                    where i.IsGenericType
                    where i.GetGenericTypeDefinition() == typeof (IHandleDomainEvents<>)
                    select i.GetGenericArguments()[0]).ToList();
        }
    }
}