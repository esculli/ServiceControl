using System;

    using NServiceBus;
using NServiceBus.Saga;

class Program
{
    static void Main()
    {
        Configure.Serialization.Json();

        using (var bus = Configure.With()
            .DefaultBuilder()
            .InMemorySagaPersister()
            .InMemoryFaultManagement()
            .UnicastBus()
            .CreateBus())
        {
            bus.Start();
        }
        Console.WriteLine("\r\nPress any key to stop program\r\n");
        Console.Read();
    }
}

public class MySaga:Saga<MySagaData>
{}

public class MySagaData : IContainSagaData
{
    public Guid Id { get; set; }
    public string Originator { get; set; }
    public string OriginalMessageId { get; set; }
}