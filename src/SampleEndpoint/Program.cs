using System;
    using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation.Environments;

class Program
{
    static void Main()
    {
        Configure.Serialization.Json();

        Feature.Enable<Sagas>();
        using (var bus = Configure.With()
            .DefaultBuilder()
            .RavenPersistence()
            .UnicastBus()
            .CreateBus())
        {
            bus.Start(() => Configure.Instance.ForInstallationOn<Windows>().Install());
            bus.SendLocal(new MyMessage
            {
                SomeId = Guid.NewGuid()
            });
        }
        Console.WriteLine("\r\nPress any key to stop program\r\n");
        Console.Read();
    }
}