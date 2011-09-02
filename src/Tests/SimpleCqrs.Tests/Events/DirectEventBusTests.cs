﻿using System;
using System.Collections.Generic;
using AutoMoq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleCqrs.Eventing;

namespace SimpleCqrs.Core.Tests.Events
{
    [TestClass]
    public class DirectEventBusTests
    {
        private AutoMoqer mocker;

        [TestInitialize]
        public void SetupMocksForAllTests()
        {
            mocker = new AutoMoqer();
        }

        [TestMethod]
        public void DomainEventHandlerForMyTestEventIsCalledWhenHandlerTypeIsInTypeCatalog()
        {
            mocker.GetMock<IDomainEventHandlerFactory>()
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns((Type type) => Activator.CreateInstance(type));

            mocker.GetMock<ITypeCatalog>()
                .Setup(typeCatalog => typeCatalog.GetGenericInterfaceImplementations(typeof(IHandleDomainEvents<>)))
                .Returns(new[] { typeof(MyTestEventHandler) });

            var eventBus = CreateLocalEventBus();
            var myTestEvent = new MyTestEvent();

            eventBus.PublishEvent(myTestEvent);

            Assert.AreEqual(101, myTestEvent.Result);
        }

        [TestMethod]
        public void DomainEventHandlerThatImplementsTwoHandlersAreCalledWhenHandlerTypeIsInTypeCatalog()
        {
            mocker.GetMock<IDomainEventHandlerFactory>()
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns((Type type) => Activator.CreateInstance(type));

            mocker.GetMock<ITypeCatalog>()
                .Setup(typeCatalog => typeCatalog.GetGenericInterfaceImplementations(typeof(IHandleDomainEvents<>)))
                .Returns(new[] { typeof(MyTest2EventHandler) });

            var eventBus = CreateLocalEventBus();
            var myTestEvent = new MyTestEvent();
            var myTest2Event = new MyTest2Event();

            eventBus.PublishEvents(new DomainEvent[] { myTestEvent, myTest2Event });

            Assert.AreEqual(102, myTestEvent.Result);
            Assert.AreEqual(45, myTest2Event.Result);
        }

        [TestMethod]
        public void AllEventHandlersAreCalledWhenHandlerTypesAreInTheTypeCatalog()
        {
            mocker.GetMock<IDomainEventHandlerFactory>()
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns((Type type) => Activator.CreateInstance(type));

            mocker.GetMock<ITypeCatalog>()
                .Setup(typeCatalog => typeCatalog.GetGenericInterfaceImplementations(typeof(IHandleDomainEvents<>)))
                .Returns(new[] { typeof(MyTestEventHandler), typeof(MyTest2EventHandler) });

            var eventBus = CreateLocalEventBus();
            var myTestEvent = new MyTestEvent();

            eventBus.PublishEvent(myTestEvent);

            Assert.IsTrue(myTestEvent.MyTestEventHandlerWasCalled);
            Assert.IsTrue(myTestEvent.MyTest2EventHandlerWasCalled);
        }

        [TestMethod]
        public void EventsAreConvertedAndEventHandlersAreCalled()
        {
            var timesCalled = 0;

            mocker.GetMock<IHandleDomainEvents<MyTestEvent>>()
                .Setup(_ => _.Handle(It.IsAny<MyTestEvent>()))
                .Callback(() => timesCalled++);

            mocker.GetMock<IDomainEventHandlerFactory>()
                .Setup(factory => factory.Create(It.IsAny<Type>()))
                .Returns((Type type) => mocker.Resolve<IHandleDomainEvents<MyTestEvent>>());

            mocker.GetMock<IEventConverter<MyTestEvent_v1, MyTestEvent_v2>>()
                .Setup(converter => converter.Convert(It.IsAny<MyTestEvent_v1>()))
                .Returns(new MyTestEvent_v2());

            mocker.GetMock<IDomainEventConverterFactory>()
                .Setup(factory => factory.Create(mocker.Resolve<IEventConverter<MyTestEvent_v1, MyTestEvent_v2>>().GetType()))
                .Returns((Type type) => mocker.Resolve<IEventConverter<MyTestEvent_v1, MyTestEvent_v2>>());

            mocker.GetMock<IEventConverter<MyTestEvent_v2, MyTestEvent>>()
                .Setup(converter => converter.Convert(It.IsAny<MyTestEvent_v2>()))
                .Returns(new MyTestEvent());

            mocker.GetMock<IDomainEventConverterFactory>()
                .Setup(factory => factory.Create(mocker.Resolve<IEventConverter<MyTestEvent_v2, MyTestEvent>>().GetType()))
                .Returns((Type type) => mocker.Resolve<IEventConverter<MyTestEvent_v2, MyTestEvent>>());

            mocker.GetMock<ITypeCatalog>()
                .Setup(typeCatalog => typeCatalog.GetGenericInterfaceImplementations(typeof(IHandleDomainEvents<>)))
                .Returns(new[] { typeof(MyTestEventHandler), typeof(MyTest2EventHandler) });

            var eventBus = new LocalEventBus(new [] {mocker.Resolve<IHandleDomainEvents<MyTestEvent>>().GetType()}, 
                mocker.Resolve<IDomainEventHandlerFactory>(),
                new [] { mocker.Resolve<IEventConverter<MyTestEvent_v1, MyTestEvent_v2>>().GetType(), mocker.Resolve<IEventConverter<MyTestEvent_v2, MyTestEvent>>().GetType() },
                mocker.Resolve<IDomainEventConverterFactory>());

            eventBus.PublishEvents(new DomainEvent[] { new MyTestEvent(), new MyTestEvent_v1(), new MyTestEvent_v2() });

            Assert.AreEqual(3, timesCalled);
        }


        private LocalEventBus CreateLocalEventBus()
        {
            var typeCatalog = mocker.Resolve<ITypeCatalog>();
            var handlerFactory = mocker.Resolve<IDomainEventHandlerFactory>();
            var converterFactory = mocker.Resolve<IDomainEventConverterFactory>();
            var eventHandlerTypes = typeCatalog.GetGenericInterfaceImplementations(typeof(IHandleDomainEvents<>));
            var eventConverterTypes = typeCatalog.GetGenericInterfaceImplementations(typeof(IEventConverter<,>));

            return new LocalEventBus(eventHandlerTypes, handlerFactory, eventConverterTypes, converterFactory);
        }
    }

    public class MyTestEventHandler : IHandleDomainEvents<MyTestEvent>
    {
        public void Handle(MyTestEvent domainEvent)
        {
            domainEvent.Result = 101;
            domainEvent.MyTestEventHandlerWasCalled = true;
        }
    }

    public class MyTest2EventHandler : IHandleDomainEvents<MyTestEvent>, IHandleDomainEvents<MyTest2Event>
    {
        public void Handle(MyTest2Event domainEvent)
        {
            domainEvent.Result = 45;
        }

        public void Handle(MyTestEvent domainEvent)
        {
            domainEvent.Result = 102;
            domainEvent.MyTest2EventHandlerWasCalled = true;
        }
    }

    public class MyTestEvent : DomainEvent
    {
        public virtual int Result { get; set; }
        public bool MyTestEventHandlerWasCalled { get; set; }
        public bool MyTest2EventHandlerWasCalled { get; set; }
    }

    public class MyTest2Event : DomainEvent
    {
        public int Result { get; set; }
    }

    public class MyTestEvent_v2 : DomainEvent
    {
        public virtual int Result { get; set; }
        public bool MyTestEventHandlerWasCalled { get; set; }
        public bool MyTest2EventHandlerWasCalled { get; set; }
        public string DepricatedString { get; set; }
    }

    public class MyTestEvent_v1 : DomainEvent
    {
        public virtual int Result { get; set; }
        public bool MyTestEventHandlerWasCalled { get; set; }
        public bool MyTest2EventHandlerWasCalled { get; set; }
        public string DepricatedString { get; set; }
        public string SuperDepricatedString { get; set; }
    }

    public class MyTestEventConverter : 
        IEventConverter<MyTestEvent_v1, MyTestEvent_v2>,
        IEventConverter<MyTestEvent_v2, MyTestEvent>
    {
        public MyTestEvent_v2 Convert(MyTestEvent_v1 sourceEvent)
        {
            return new MyTestEvent_v2
                       {
                           Result = sourceEvent.Result,
                           MyTestEventHandlerWasCalled = sourceEvent.MyTestEventHandlerWasCalled,
                           MyTest2EventHandlerWasCalled = sourceEvent.MyTest2EventHandlerWasCalled,
                           AggregateRootId = sourceEvent.AggregateRootId,
                           EventDate = sourceEvent.EventDate,
                           Sequence = sourceEvent.Sequence,
                           DepricatedString = "Old"
                       };
        }

        public MyTestEvent Convert(MyTestEvent_v2 sourceEvent)
        {
            return new MyTestEvent
                       {
                           Result = sourceEvent.Result,
                           MyTestEventHandlerWasCalled = sourceEvent.MyTestEventHandlerWasCalled,
                           MyTest2EventHandlerWasCalled = sourceEvent.MyTest2EventHandlerWasCalled,
                           AggregateRootId = sourceEvent.AggregateRootId,
                           EventDate = sourceEvent.EventDate,
                           Sequence = sourceEvent.Sequence,
                       };
        }
    }

    public class MockServiceLocator : IServiceLocator
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public T Resolve<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public T Resolve<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Func<Type, object> ResolveFunc { get; set; }

        public object Resolve(Type type)
        {
            return ResolveFunc(type);
        }

        public IList<T> ResolveServices<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void Register<TInterface>(Type implType) where TInterface : class
        {
            throw new NotImplementedException();
        }

        public void Register<TInterface, TImplementation>() where TImplementation : class, TInterface
        {
            throw new NotImplementedException();
        }

        public void Register<TInterface, TImplementation>(string key) where TImplementation : class, TInterface
        {
            throw new NotImplementedException();
        }

        public void Register(string key, Type type)
        {
            throw new NotImplementedException();
        }

        public void Register(Type serviceType, Type implType)
        {
            throw new NotImplementedException();
        }

        public void Register<TInterface>(TInterface instance) where TInterface : class
        {
            throw new NotImplementedException();
        }

        public void Release(object instance)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public TService Inject<TService>(TService instance) where TService : class
        {
            throw new NotImplementedException();
        }

        public void TearDown<TService>(TService instance) where TService : class
        {
            throw new NotImplementedException();
        }

        public void Register<Interface>(Func<Interface> factoryMethod) where Interface : class
        {
            throw new NotImplementedException();
        }
    }
}