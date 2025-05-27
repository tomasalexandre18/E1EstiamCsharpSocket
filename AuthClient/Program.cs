using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using JsonModele;

namespace AuthClient;



class Program
{
    private static bool _Authenticated = false;
    private static int _ChannelSelected = -1;
    
    public static async Task Main(string[] args)
    {
        var client = new TcpClient();
        await client.ConnectAsync("localhost", 5000);
        client.NoDelay = true;
        Console.WriteLine("Connected to the server!");
        // get if the user want to login or register
        Console.WriteLine("Do you want to login or register? (l/r)");
        var choice = Console.ReadLine()?.ToLower();
        if (choice == "l")
        {
            while (!_Authenticated )
            {
                await Login(client);
            }
        }
        else if (choice == "r")
        {
            await Register(client);
            while (!_Authenticated )
            {
                await Login(client);
            }
        }
        else
        {
            Console.WriteLine("Invalid choice. Exiting...");
            return;
        }
        // menu list channels / select chanell / create channel / (send message / get old messages if channel selected)
        while (true)
        {
            Console.WriteLine("Menu:");
            Console.WriteLine("1. List channels");
            Console.WriteLine("2. Create channel");
            Console.WriteLine("3. Select channel");
            if (_ChannelSelected != -1)
            {
                Console.WriteLine("4. Send message");
                Console.WriteLine("5. Get old messages");
            }
            else
            {
                Console.WriteLine("4. (Send message disabled, no channel selected)");
                Console.WriteLine("5. (Get old messages disabled, no channel selected)");
            }
            Console.WriteLine("6. Exit");
            var menuChoice = Console.ReadLine();
            switch (menuChoice)
            {
                case "1":
                    await ListChannels(client);
                    break;
                case "2":
                    await CreateChannel(client);
                    break;
                case "3":
                    await SelectChannel(client);
                    break;
                case "4":
                    await SendMessage(client);
                    break;
                case "5":
                    await GetOldMessages(client);
                    break;
                case "6":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }
    
    private static async Task ListChannels(TcpClient client)
    {
        var response = await SendGetPacket(client, "channel/listchannels", new { });
        if (response == null)
        {
            Console.WriteLine("Failed to list channels. No response from server.");
            return;
        }
        if (response.code != 200)
        {
            Console.WriteLine($"Failed to list channels: {response.message}");
            return;
        }
        var channels = JsonSerializer.Deserialize<List<ChannelInfo>>(response.data?.ToString() ?? string.Empty);
        if (channels == null || channels.Count == 0)
        {
            Console.WriteLine("No channels available.");
            return;
        }
        Console.WriteLine("Available channels:");
        foreach (var channel in channels)
        {
            Console.WriteLine($"ID: {channel.id}, Name: {channel.name}, Description: {channel.description}, Created By: {channel.createdBy}, Created At: {channel.createdAt}");
        }
    }
    
    private static async Task CreateChannel(TcpClient client)
    {
        Console.Write("Enter channel name: ");
        var name = Console.ReadLine();
        Console.Write("Enter channel description: ");
        var description = Console.ReadLine();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
        {
            Console.WriteLine("Channel name and description cannot be empty.");
            return;
        }
        var data = new CreateChannel
        {
            name = name,
            description = description
        };
        var response = await SendGetPacket(client, "channel/createchannel", data);
        if (response == null)
        {
            Console.WriteLine("Failed to create channel. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            var channelResponse = JsonSerializer.Deserialize<ResponseChannelCreate>(response.data?.ToString() ?? string.Empty);
            if (channelResponse != null)
            {
                Console.WriteLine($"Channel created successfully! Channel ID: {channelResponse.channelId}");
            }
            else
            {
                Console.WriteLine("Failed to parse channel creation response.");
            }
        }
        else
        {
            Console.WriteLine($"Failed to create channel: {response.message}");
        }
    }
    
    private static async Task SelectChannel(TcpClient client)
    {
        Console.Write("Enter channel ID to select: ");
        if (!int.TryParse(Console.ReadLine(), out var channelId))
        {
            Console.WriteLine("Invalid channel ID.");
            return;
        }
        var response = await SendGetPacket(client, "channel/getchannel", new InfoChannelById
        {
            channelId = channelId
        });
        if (response == null)
        {
            Console.WriteLine("Failed to select channel. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            _ChannelSelected = channelId;
            Console.WriteLine($"Channel {channelId} selected successfully!");
        }
        else
        {
            Console.WriteLine($"Failed to select channel: {response.message}");
        }
    }
    
    private static async Task SendMessage(TcpClient client)
    {
        if (_ChannelSelected == -1)
        {
            Console.WriteLine("No channel selected. Please select a channel first.");
            return;
        }
        Console.Write("Enter your message: ");
        var message = Console.ReadLine();
        if (string.IsNullOrEmpty(message))
        {
            Console.WriteLine("Message cannot be empty.");
            return;
        }
        var data = new SendMessage
        {
            channelId = _ChannelSelected,
            message = message
        };
        var response = await SendGetPacket(client, "message/sendmessage", data);
        if (response == null)
        {
            Console.WriteLine("Failed to send message. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            Console.WriteLine("Message sent successfully!");
        }
        else
        {
            Console.WriteLine($"Failed to send message: {response.message}");
        }
    }
    
    private static async Task GetOldMessages(TcpClient client)
    {
        if (_ChannelSelected == -1)
        {
            Console.WriteLine("No channel selected. Please select a channel first.");
            return;
        }
        var data = new ListMessages
        {
            channelId = _ChannelSelected
        };
        var response = await SendGetPacket(client, "message/getoldmessages", data);
        if (response == null)
        {
            Console.WriteLine("Failed to get old messages. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            var messages = JsonSerializer.Deserialize<List<MessageResponse>>(response.data?.ToString() ?? string.Empty);
            if (messages != null && messages.Count > 0)
            {
                Console.WriteLine("Old messages:");
                foreach (var msg in messages)
                {
                    Console.WriteLine($"[{msg.timestamp}] {msg.username}: {msg.content}");
                }
            }
            else
            {
                Console.WriteLine("No old messages found.");
            }
        }
        else
        {
            Console.WriteLine($"Failed to get old messages: {response.message}");
        }
    }

    private static async Task Register(TcpClient client)
    {
        // get username and password
        Console.Write("Enter your username: ");
        var username = Console.ReadLine();
        Console.Write("Enter your password: ");
        var password = Console.ReadLine();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Username and password cannot be empty.");
            return;
        }
        var data = new RegisterUser
        {
            username = username,
            password = password
        };
        var response = await SendGetPacket(client, "auth/register", data);
        if (response == null)
        {
            Console.WriteLine("Failed to register. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            Console.WriteLine("Registration successful!");
        }
        else
        {
            Console.WriteLine($"Registration failed: {response.message}");
        }
    }
    
    private static async Task Login(TcpClient client)
    {
        // get username and password
        Console.Write("Enter your username: ");
        var username = Console.ReadLine();
        Console.Write("Enter your password: ");
        var password = Console.ReadLine();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Username and password cannot be empty.");
            return;
        }
        var data = new AuthCheck
        {
            username = username,
            password = password
        };
        var response = await SendGetPacket(client, "auth/login", data);
        if (response == null)
        {
            Console.WriteLine("Failed to login. No response from server.");
            return;
        }
        if (response.code == 200)
        {
            Console.WriteLine("Login successful!");
            _Authenticated = true;
            return;
        }
        Console.WriteLine($"Login failed: {response.message}");
        
    }
    
    private static async Task<PacketResponse?> SendGetPacket(TcpClient client, string route, object data)
    {
        var packet = new Packet()
        {
            uri = route,
            data = JsonSerializer.SerializeToElement(data)
        };
        Console.WriteLine($"Sending packet: {JsonSerializer.Serialize(packet)}");
        await SendJsonMessage(client, packet);
        var response = await ReadJsonAsync(client.GetStream());
        if (response == null)
        {
            Console.WriteLine("No response received from the server.");
            return null;
        }
        Console.WriteLine($"Received response: {JsonSerializer.Serialize(response)}");
        return response;
    }
    
    private static async Task SendJsonMessage(TcpClient client, Packet packet)
    {
        if (client.Connected)
        {
            var stream = client.GetStream();
            var data = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));
            var header = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(header, 0, header.Length);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }
        else
        {
            Console.WriteLine("Client disconnected.");
        }
    }
    
    private static async Task<PacketResponse?> ReadJsonAsync(NetworkStream stream)
    {
        var bytesHeaderLength = new byte[4];
        var bytesRead = await stream.ReadAsync(bytesHeaderLength, 0, 4);
        if (bytesRead == 0)
        {
            return null;
        }
        var headerLength = BitConverter.ToInt32(bytesHeaderLength, 0);
        var bytes = new byte[headerLength];
        bytesRead = await stream.ReadAsync(bytes, 0, headerLength);
        if (bytesRead == 0)
        {
            return null;
        }
        var json = System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRead);
        return JsonSerializer.Deserialize<PacketResponse>(json);
    }
}