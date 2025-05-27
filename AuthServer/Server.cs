using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using AuthServer;
using JsonModele;

namespace JSONServer;
class Connection
{
    public TcpClient Client { get; set; }
    public string Name { get; set; }
    public bool isAuth { get; set; }
}

class RouteEntry
{
    public Func<TcpClient, object, Task> Handler { get; set; }
    public Type DataType { get; set; }

    public bool NeedAuthentication = false;
}

class ConfigRoute
{
    public bool NeedAuthentication { get; set; } = false;
}
static class Server {
    
    public static readonly Dictionary<int, Connection> Clients = new();
    public static readonly DatabaseContext DataBase = new DatabaseContext();
    private static Dictionary<string, RouteEntry> Routes = new();
    
    
    public static async Task lunchServer(string ip = "0.0.0.0", int port = 5000)
    {
        var server = new TcpListener(IPAddress.Parse("0.0.0.0"), 5000);
        server.Start();
        
        await GestionConnect(server);
    }
    
    
    public static async Task SendJsonResponse(TcpClient client, int code, string message, object? data = null)
    {
        var response = new PacketResponse
        {
            code = code,
            message = message,
            data = data
        };
        await SendJsonMessage(client, JsonSerializer.Serialize(response));
    }
    
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        // sha256 hash the password
        var hashedInputPassword = HashPassword(password);
        return hashedInputPassword == hashedPassword;
    }
    
    public static string HashPassword(string password)
    {
        // sha256 hash the password
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)).ToString()!;
    }

    private static async Task GestionConnect(TcpListener server)
    {
        try
        {
            while (true)
            {
                Console.Write("Waiting for a connection... ");
                var client = await server.AcceptTcpClientAsync();
                client.NoDelay = true;
                Console.WriteLine("Connected from {0}!", ((IPEndPoint)client.Client.RemoteEndPoint!).Address);
                var uniqueId = Random.Shared.Next(1000, 9999);
                while (Clients.ContainsKey(uniqueId))
                {
                    uniqueId = Random.Shared.Next(1000, 9999);
                }
                var connection = new Connection
                {
                    Client = client,
                    Name = $"anonymous",
                    isAuth = false
                };
                Clients.Add(uniqueId, connection);
                _ = Task.Run(() => GestionSocket(client, uniqueId));
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server.Stop();
        }
    }
    
    private static async Task SendInvalidObjectFormat(TcpClient client)
    {
        var response = new PacketResponse
        {
            code = 400,
            message = "Invalid JSON object format."
        };
        await SendJsonMessage(client, JsonSerializer.Serialize(response));
    }

    private static async Task GestionSocket(TcpClient client, int uniqueId)
    {
        var stream = client.GetStream();
        while (true)
        {
            var json = await ReadJsonAsync(client);
            if (json == false)
            {
                await SendInvalidObjectFormat(client);
                Console.WriteLine("Client disconnected or invalid JSON received.");
                Clients.Remove(uniqueId);
                return;
            }
        }
    }

    public static void AddRoute<T>(string route, Func<TcpClient, T, Task> handler, ConfigRoute? config = null) where T : class
    {
        if (Routes.ContainsKey(route))
        {
            Console.WriteLine($"Route {route} already exists.");
            return;
        }
        async Task Wrapper(TcpClient client, object data)
        {
            if (data is not T typedData)
            {
                await SendInvalidObjectFormat(client);
                return;
            }

            await handler(client, typedData);
        }

        Routes[route] = new RouteEntry
        {
            Handler = Wrapper,
            DataType = typeof(T),
            NeedAuthentication = config?.NeedAuthentication ?? false
        };
        
        Console.WriteLine($"Route {route} added successfully.");
    }
    
    private static async Task<bool> ReadJsonAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var bytesHeaderLength = new byte[4];
        var bytesRead = await stream.ReadAsync(bytesHeaderLength.AsMemory(0, 4));
        if (bytesRead == 0)
        {
            return false;
        }
        var headerLength = BitConverter.ToInt32(bytesHeaderLength, 0);
        var bytes = new byte[headerLength];
        bytesRead = await stream.ReadAsync(bytes.AsMemory(0, headerLength));
        if (bytesRead == 0)
        {
            return false;
        }
        var json = System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRead);
        try
        {
            var packet = JsonSerializer.Deserialize<Packet>(json);
            var routeEntry = Routes.GetValueOrDefault(packet?.uri);
            if (routeEntry != null)
            {
                if (routeEntry.NeedAuthentication && !IsAuthenticated(client))
                {
                    Console.WriteLine($"Client {client.Client.RemoteEndPoint} is not authenticated for route: {packet?.uri}");
                    await SendJsonMessage(client, JsonSerializer.Serialize(new PacketResponse
                    {
                        code = 401,
                        message = "Authentication required."
                    }));
                    return true;
                }
                await routeEntry.Handler(client, packet.data?.Deserialize(routeEntry.DataType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
                return true;
            }
            Console.WriteLine($"No handler found for route: {packet?.uri}");
            await SendJsonMessage(client, JsonSerializer.Serialize(new PacketResponse
            {
                code = 404,
                message = "Route not found."
            }));
            return true;
        } catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            return false;
        }
    }


    private static bool IsAuthenticated(TcpClient client)
    {
        // Check if the client is authenticated
        var uniqueId = Clients.FirstOrDefault(c => c.Value.Client == client).Key;
        return uniqueId != 0 && Clients[uniqueId].isAuth;
    }
    
    private static async Task SendJsonMessage(TcpClient client, string message)
    {
        if (!client.Connected) return;
        var stream = client.GetStream();
        var data = System.Text.Encoding.UTF8.GetBytes(message);
        var header = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(header);
        await stream.WriteAsync(data);
        await stream.FlushAsync();
    }
    
    
}