using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SimpleCqrs.Eventing;

namespace SimpleCqrs.EventStore.MongoDb
{
    public class MongoEventStore : IEventStore
    {
        readonly MongoCollection<DomainEvent> _collection;
        readonly Dictionary<string, string> _eventHashRef;

        public MongoEventStore(string connectionString, ITypeCatalog typeCatalog)
        {
            _eventHashRef = new Dictionary<string, string>();
            typeCatalog.GetDerivedTypes(typeof(DomainEvent)).ToList().
                ForEach(x => BsonClassMap.RegisterClassMap(new DomainEventMapper(x, _eventHashRef)));

            _collection = MongoServer.Create(connectionString).GetDatabase("events").GetCollection<DomainEvent>("events");
        }

        public IEnumerable<DomainEvent> GetEvents(Guid aggregateRootId, int startSequence)
        {
            return _collection.Find(
                Query.And(
                    Query.EQ("AggregateRootId", aggregateRootId), 
                    Query.GT("Sequence", startSequence)))
                .ToList();
        }

        public void Insert(IEnumerable<DomainEvent> domainEvents)
        {
            _collection.InsertBatch(domainEvents);
        }

        public IEnumerable<DomainEvent> GetEventsByEventTypes(IEnumerable<Type> domainEventTypes, DateTime startDate, DateTime endDate)
        {
            return _collection.Find(
                Query.And(
                    Query.In("_t", domainEventTypes.Select(t => new BsonString(_eventHashRef.SingleOrDefault(_ => _.Key == t.FullName).Value ?? t.Name)).ToArray()), 
                    Query.GTE("EventDate", startDate),
                    Query.LTE("EventDate", endDate)))
                .ToList();
        }

        public IEnumerable<DomainEvent> GetEventsBySelector(IMongoQuery selector, int skip, int limit)
        {
            return _collection.Find(selector).SetSkip(skip).SetLimit(limit);
        }

    }
}