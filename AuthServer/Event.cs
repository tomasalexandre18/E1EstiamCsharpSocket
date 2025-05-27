using System.Net.Sockets;

namespace AuthServer;

public class EventEntry
{
    public Func<TcpClient, Task> Handler { get; set; }
    public string id { get; set; }
}
public static class Event
{
    private static readonly Dictionary<string, List<EventEntry>> EventStorage = new();
    
    public static void AddEvent(string eventName, Func<TcpClient, Task> eventHandler)
    {
        if (!EventStorage.ContainsKey(eventName))
        {
            EventStorage[eventName] = new List<EventEntry>();
        }
        var randomId = eventName + "-" + new Random().Next(1, int.MaxValue);
        EventStorage[eventName].Add(new EventEntry{id = randomId, Handler = eventHandler});
    }
    public static async Task TriggerEvent(string eventName, TcpClient client)
    {
        if (EventStorage.TryGetValue(eventName, out var value))
        {
            foreach (var eventEntry in value)
            {
                await eventEntry.Handler(client);
            }
        }
        else
        {
            Console.WriteLine($"No handlers found for event: {eventName}");
        }
    }
    
    public static void RemoveEvent(string id)
    {
        foreach (var eventList in EventStorage.Values)
        {
            var entryToRemove = eventList.FirstOrDefault(e => e.id == id);
            if (entryToRemove == null) continue;
            eventList.Remove(entryToRemove);
            return; // Exit after removing the first matching entry
        }
        Console.WriteLine($"No event found with id: {id}");
    }
}