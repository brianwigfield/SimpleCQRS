using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace SimpleCqrs.EventStore.MongoDb
{
    public class DomainEventMapper : BsonClassMap
    {
        public DomainEventMapper(Type classType, Dictionary<string, string> eventHashRef)
            : base(classType)
        {
            AutoMap();

            var hashMethod = classType.GetMethod("GetHashCode");
            if (hashMethod.DeclaringType == classType)
            {
                var hash = hashMethod.Invoke(Activator.CreateInstance(classType), null).ToString();
                SetDiscriminator(hash);
                eventHashRef.Add(classType.FullName, hash);
            }

            SetIgnoreExtraElements(true);
        }
    }
}