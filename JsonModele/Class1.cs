using System.Text.Json;

namespace JsonModele;

public class AuthCheck
{
    public string username { get; set; }
    public string password { get; set; }
}

public class AuthReturn
{
    public int code { get; set; }
    public string message { get; set; }
}

public class UserInfo
{
    public string username { get; set; }
}

public class RegisterUser
{
    public string username { get; set; }
    public string password { get; set; }
}

public class CreateChannel
{
    public string name { get; set; }
    public string description { get; set; }
}

public class ResponseChannelCreate
{
    public int channelId { get; set; }
}

public class ListChannels
{
    public List<ChannelInfo> channels { get; set; }
}

public class ChannelInfo
{
    public int id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    
    public string createdBy { get; set; } // Username of the creator
    
    public DateTime createdAt { get; set; } // Creation date
}


public class Packet
{
    public string uri { get; set; }
    public JsonElement? data { get; set; }
}

public class SendMessage
{
    public int channelId { get; set; }
    public string message { get; set; }
}

public class MessageResponse
{ 
    public string content { get; set; }
    public DateTime timestamp { get; set; }
    public string username { get; set; } // Username of the sender
}

public class ListMessageResponse
{
    public List<MessageResponse> messages { get; set; }
}
public class ListMessages
{
    public int channelId { get; set; }
}


public class ErrorResponse
{
    public int code { get; set; }
    public string message { get; set; }
}

public class PacketResponse
{
    public int code { get; set; }
    public string? message { get; set; }
    public object? data { get; set; }
}

public class InfoChannelById
{
    public int channelId { get; set; }
}