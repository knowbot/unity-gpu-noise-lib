using System.Collections.Generic;

public interface ISubject
{
    List<IObserver> Observers { get; }
    void Attach(IObserver observer);
    void Detach(IObserver observer);
    void NotifyAll();
}