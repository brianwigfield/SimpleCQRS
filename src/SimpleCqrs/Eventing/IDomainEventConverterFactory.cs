using System;

namespace SimpleCqrs.Eventing
{
    public interface IDomainEventConverterFactory
    {
        object Create(Type domainEventHandlerType);
    }
}