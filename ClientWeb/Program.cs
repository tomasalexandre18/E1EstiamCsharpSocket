using System.Net.Sockets;

namespace ClientWeb;

internal static class ClientWeb
{
    private static bool _isOpen = true;
    
    public static void Main(string[] args)
    {
        Connect("localhost", 5000);
    }

    private static void Connect(string server, int port)
    {
        try
        {
            using var client = new TcpClient(server, port);
            client.NoDelay = true;
            
            var th = new Thread(() => handleReception(client));
            th.Start();
            
            var stream = client.GetStream();
            Thread.Sleep(10);
            while (true)
            {
                var dataToSend = Console.ReadLine();
                if (dataToSend == null) continue;
                switch (dataToSend)
                {
                    case "exit":
                        Console.WriteLine("Shutting down...");
                        _isOpen = false;
                        return;
                    case "clear":
                        Console.Clear();
                        continue;
                }
                
                sendMessage(client, dataToSend);
                Thread.Sleep(10);
            }
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException: {0}", e);
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
    }
    
    private static void handleReception(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            int i;
            var bytes = new byte[1000];

            // Loop to receive all the data sent by the server.
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                var responseData = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                Console.WriteLine(responseData);
            }
        }
        catch (Exception e)
        {
            if (!_isOpen)
            {
                Console.WriteLine("Client connection closed.");
                return;
            }
            Console.WriteLine("Exception: {0}", e);
        }
    }
    
    private static void sendMessage(TcpClient client, string message)
    {
        if (!client.Connected) return;
        try
        {
            var stream = client.GetStream();
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error sending message: {0}", e);
        }
    }
}