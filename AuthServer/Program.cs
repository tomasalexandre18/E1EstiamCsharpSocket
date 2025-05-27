using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using JsonModele;
using JSONServer;
using Microsoft.EntityFrameworkCore;

namespace AuthServer;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // add routes
        Server.AddRoute<AuthCheck>("auth/login", LoginRoute);
        Server.AddRoute<RegisterUser>("auth/register", RegisterRoute);
        Server.AddRoute<object>("auth/userinfo", UserInfoRoute, new ConfigRoute { NeedAuthentication = true });
        
        Server.AddRoute<object>("channel/listchannels", ListChannelRoute, new ConfigRoute { NeedAuthentication = true });
        Server.AddRoute<CreateChannel>("channel/createchannel", CreateChannelRoute, new ConfigRoute { NeedAuthentication = true });
        Server.AddRoute<InfoChannelById>("channel/getchannel", GetChannelRoute, new ConfigRoute { NeedAuthentication = true });
        
        Server.AddRoute<ListMessages>("message/getoldmessages", GetOldMessagesRoute, new ConfigRoute { NeedAuthentication = true });
        Server.AddRoute<SendMessage>("message/sendmessage", SendMessageRoute, new ConfigRoute { NeedAuthentication = true });

        await Server.lunchServer();
    }

    private static async Task LoginRoute(TcpClient client, AuthCheck data)
    {
        Console.WriteLine($"{client.Client.RemoteEndPoint}: {JsonSerializer.Serialize(data)}");

        var user = await Server.DataBase.Users.FirstOrDefaultAsync(u => u.Username == data.username);
        if (user == null || !Server.VerifyPassword(data.password, user.Password))
        {
            await Server.SendJsonResponse(client, 401, "Invalid username or password.");
            return;
        }

        var uniqueId = Server.Clients.FirstOrDefault(c => c.Value.Client == client).Key;


        Server.Clients[uniqueId].isAuth = true;
        Server.Clients[uniqueId].Name = user.Username;
        await Server.SendJsonResponse(client, 200, "Login successful.", new UserInfo
        {
            username = user.Username
        });
    }

    private static async Task RegisterRoute(TcpClient client, RegisterUser data)
    {
        var existingUser = await Server.DataBase.Users.FirstOrDefaultAsync(u => u.Username == data.username);
        if (existingUser != null)
        {
            await Server.SendJsonResponse(client, 400, "Username already exists.");
        }

        var hashedPassword = Server.HashPassword(data.password);
        var newUser = new User
        {
            Username = data.username,
            Password = hashedPassword
        };

        Server.DataBase.Users.Add(newUser);
        await Server.DataBase.SaveChangesAsync();

        await Server.SendJsonResponse(client, 200, "User registered successfully.");
    }


    private static async Task UserInfoRoute(TcpClient client, object _)
    {
        var username = Server.Clients.FirstOrDefault(c => c.Value.Client == client).Value.Name;
        var user = await Server.DataBase.Users.FirstOrDefaultAsync(u => u.Username == username);
        await Server.SendJsonResponse(client, 200, "User info retrieved successfully.", new UserInfo
        {
            username = user?.Username ?? "anonymous"
        });
    }

    private static async Task ListChannelRoute(TcpClient client, object _)
    {
        var channels = await Server.DataBase.Channels.ToListAsync();
        var channelList = channels.Select(c => new ChannelInfo
        {
            id = c.Id,
            name = c.Name,
            description = c.Description,
            createdBy = c.CreateBy.Username,
            createdAt = c.CreateAt
        }).ToList();

        await Server.SendJsonResponse(client, 200, "Channel list retrieved successfully.", channelList);
    }
    
    private static async Task GetChannelRoute(TcpClient client, InfoChannelById data)
    {
        var channel = await Server.DataBase.Channels
            .Where(c => c.Id == data.channelId)
            .Select(c => new ChannelInfo
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                createdBy = c.CreateBy.Username,
                createdAt = c.CreateAt
            }).FirstOrDefaultAsync();

        if (channel == null)
        {
            await Server.SendJsonResponse(client, 404, "Channel not found.");
            return;
        }

        await Server.SendJsonResponse(client, 200, "Channel retrieved successfully.", channel);
    }

    private static async Task CreateChannelRoute(TcpClient client, CreateChannel data)
    {
        var uniqueId = Server.Clients.FirstOrDefault(c => c.Value.Client == client).Key;
        var creator = Server.Clients[uniqueId].Name;

        var newChannel = new Channel
        {
            Name = data.name,
            Description = data.description,
            CreateBy = await Server.DataBase.Users.FirstOrDefaultAsync(u => u.Username == creator),
            CreateAt = DateTime.UtcNow
        };

        Server.DataBase.Channels.Add(newChannel);
        await Server.DataBase.SaveChangesAsync();

        await Server.SendJsonResponse(client, 200, "Channel created successfully.",
            new ResponseChannelCreate { channelId = newChannel.Id });
    }

    private static async Task GetOldMessagesRoute(TcpClient client, ListMessages data)
    {
        var messages = await Server.DataBase.Messages
            .Where(m => m.Channel.Id == data.channelId)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => new MessageResponse
            {
                content = m.Content,
                timestamp = m.Timestamp,
                username = m.User.Username
            }).ToListAsync();
        
        await Server.SendJsonResponse(client, 200, "Messages retrieved successfully.", messages);
    }
    
    public static async Task SendMessageRoute(TcpClient client, SendMessage data)
    {
        var uniqueId = Server.Clients.FirstOrDefault(c => c.Value.Client == client).Key;
        var username = Server.Clients[uniqueId].Name;

        var channel = await Server.DataBase.Channels.FirstOrDefaultAsync(c => c.Id == data.channelId);
        if (channel == null)
        {
            await Server.SendJsonResponse(client, 404, "Channel not found.");
            return;
        }

        var message = new Message
        {
            Content = data.message,
            Timestamp = DateTime.UtcNow,
            User = await Server.DataBase.Users.FirstOrDefaultAsync(u => u.Username == username),
            Channel = channel
        };

        Server.DataBase.Messages.Add(message);
        await Server.DataBase.SaveChangesAsync();

        await Server.SendJsonResponse(client, 200, "Message sent successfully.");
    }
}