using System;

namespace SimpleCqrs.Eventing
{
    public class DomainEventConverterFactory : IDomainEventConverterFactory
    {
      private readonly IServiceLocator serviceLocator;

      public DomainEventConverterFactory(IServiceLocator serviceLocator)
        {
            this.serviceLocator = serviceLocator;
        }

        public object Create(Type domainEventHandlerType)
        {
            return serviceLocator.Resolve(domainEventHandlerType);
        }
    }
}
