using System.Net.Sockets;

namespace AuthServer;

public static class Event
{
    private static readonly Dictionary<string, List<Func<TcpClient, Task>>> EventStorage = new();
    
    public static void AddEvent(string eventName, Func<TcpClient, Task> eventHandler)
    {
        if (!EventStorage.ContainsKey(eventName))
        {
            EventStorage[eventName] = new List<Func<TcpClient, Task>>();
        }
        EventStorage[eventName].Add(eventHandler);
    }
    public static async Task TriggerEvent(string eventName, TcpClient client)
    {
        if (EventStorage.ContainsKey(eventName))
        {
            foreach (var handler in EventStorage[eventName])
            {
                await handler(client);
            }
        }
        else
        {
            Console.WriteLine($"No handlers found for event: {eventName}");
        }
    }
}