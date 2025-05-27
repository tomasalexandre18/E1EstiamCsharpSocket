using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace ServerWeb;

internal static class ServerWeb
{
    private static readonly Dictionary<string, TcpClient> Clients = new();
    
    
    public static void Main()
    {
        TcpListener? server = null;
        try
        {
            server = new TcpListener(IPAddress.Parse("0.0.0.0"), 5000);
            server.Start();
                
            while(true)
            {
                Console.Write("Waiting for a connection... ");
                    
                var client = server.AcceptTcpClient();
                client.NoDelay = true;
                Console.WriteLine("Connected from {0}!", ((IPEndPoint)client.Client.RemoteEndPoint!).Address);
                var name = $"anonymous-{client.Client.RemoteEndPoint!.GetHashCode()}";
                if (Clients.ContainsKey(name))
                {
                    name = $"anonymous-{client.Client.RemoteEndPoint!.GetHashCode()}-{Clients.Count}";
                }
                Clients.Add(name, client);
                
                _ = Task.Run(() => GestionSocket(client, name));
            }
        }
        catch(SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server?.Stop();
        }
    }

    private static async Task GestionSocket(TcpClient client, string name)
    {
        try
        {
            var stream = client.GetStream();
            
            // await BroadcastMessage(name + " has connected", [client]);
            // await SendMessage(client, "Welcome to the server! Your name is: " + name);
            
            int i;
            var bytes = new byte[3000];
            // Loop to receive all the data sent by the client.
            while ((i = await stream.ReadAsync(bytes)) != 0)
            {
                // Translate data bytes to an ASCII string.
                var data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                var finalData = "";
                if (data.StartsWith("SETNAME"))
                {
                    // Change the name of the client.
                    var oldName = name;
                    name = data.Substring(7).Trim();
                    if (Clients.ContainsKey(name))
                    {
                        name = $"{name}-{Clients.Count}";
                    }
                    Clients.Remove(oldName);
                    Clients.Add(name, client);
                    await BroadcastMessage(oldName + " changed name to " + name);
                    
                }
                else if (data.StartsWith("GETNAME"))
                {
                    await SendMessage(client, "Your name is: " + name);
                }
                else if (data.StartsWith("LIST")) 
                {
                    // List all name of clients connected to the server (excluding the client itself).
                    var clientNames = Clients.Keys.ToList();
                    var finalString = "Connected clients: " + string.Join(", ", clientNames);
                    if (clientNames.Count == 0)
                    {
                        finalString = "No other clients connected.";
                    }
                    await SendMessage(client, finalString);
                }
                else if (data.StartsWith("KICK4321"))
                {
                    // KICK4321 name
                    var message = data.Substring(8).Trim();
                    var parts = message.Split(' ', 1);
                    if (parts.Length < 1)
                    {
                        continue;
                    }
                    var targetName = parts[0];
                    
                    if (Clients.TryGetValue(targetName, out var targetClient))
                    {
                        Console.WriteLine("KICK " + targetName);
                        targetClient.Close();
                        Clients.Remove(targetName);
                    }
                    else
                    {
                        await SendMessage(client, "Client " + targetName + " not found.");
                    }
                    
                }
                else if (data.StartsWith("MSG"))
                {
                    // Send a message to a specific client (MSG <name> <message>).
                    var message = data.Substring(4).Trim();
                    var parts = message.Split(' ', 2);
                    if (parts.Length < 2)
                    {
                        await SendMessage(client, "Invalid message format. Use: MSG <name> <message>");
                        continue;
                    }
                    var targetName = parts[0];
                    var messageContent = parts[1];
                    
                    if (Clients.TryGetValue(targetName, out var targetClient))
                    {
                        await SendMessage(targetClient, "WHISPER " + name + ": " + messageContent);
                        await SendMessage(client, "Message sent to " + targetName + ": " + messageContent);
                    }
                    else
                    {
                        await SendMessage(client, "Client " + targetName + " not found.");
                    }
                }
                else
                {
                    await BroadcastMessage(name + ": " + data);
                }
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0} (by: {1})", e, name);
            // remove the client from the list
            Clients.Remove(name);
        }
        finally
        {
           client.Close();
           Console.WriteLine("Client disconnected! (by: {0})", name);
            Clients.Remove(name);
            // send a message to all clients that the client has disconnected
            var finalData = name + " has disconnected";
            await BroadcastMessage(finalData);
        }
    }
    
    private static async Task BroadcastMessage(string message, List<TcpClient>? excludeClients = null)
    {
        var excluded = excludeClients?.Select(c => c.Client.RemoteEndPoint).ToHashSet();
        List<Task> tasks = [];
        
        foreach (var client in Clients.Values)
        {
            if (excluded != null && excluded.Contains(client.Client.RemoteEndPoint))
                continue;

            tasks.Add(SendMessage(client, message));
            
        }
        await Task.WhenAll(tasks);
    }

    
    private static async Task SendMessage(TcpClient client, string message)
    {
        if (!client.Connected) return;
        try
        {
            var stream = client.GetStream();
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error sending message: {0}", e);
        }
    }
}