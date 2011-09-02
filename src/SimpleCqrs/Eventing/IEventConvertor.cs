namespace SimpleCqrs.Eventing
{
    public interface IEventConverter<in TSourceEvent, out TTargetEvent>
        where TSourceEvent : DomainEvent
        where TTargetEvent : DomainEvent
    {
        TTargetEvent Convert(TSourceEvent sourceEvent);
    }
}
