/*
Soufiane Boufarache 1053961
Yong Jiang 1015693
*/

using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);
    static int msgIdCounter = 1;

    public static void start()
    {
        Console.WriteLine("Hello World from client");
        Console.WriteLine();

        //TODO: [Create endpoints and socket]

        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);

        clientSocket.Bind(clientEndPoint);

        Console.WriteLine($"Client is bound to {clientEndPoint.Address}:{clientEndPoint.Port}");
        Console.WriteLine($"Server endpoint is {serverEndPoint.Address}:{serverEndPoint.Port}");
        Console.WriteLine();

        //TODO: [Create and send HELLO]

        SendMessage(clientSocket, serverEndPoint, msgIdCounter++, MessageType.Hello, "Hello from client");

        //TODO: [Receive and print Welcome from server]

        Message? receivedMessage = ReceiveMessage(clientSocket, ref clientEndPoint);

        if (receivedMessage?.MsgType == MessageType.Welcome)
        {   
            // TODO: [Create and send DNSLookup Message]

            List<DNSRecord> dnsRecords = new List<DNSRecord>
            {
                new DNSRecord { Type = "A", Name = "www.test.com" }, // Correct
                new DNSRecord { Type = "MX", Name = "example.com" }, // Correct
                new DNSRecord { Type = "MX", Name = "nonexistent.com" }, // Incorrect
                new DNSRecord { Type = "AA", Name = "invalid.com" } // Incorrect
            };

            // TODO: [Send next DNSLookup to server]
            foreach (var dnsRecord in dnsRecords)
            {
                SendMessage(clientSocket, serverEndPoint, msgIdCounter, MessageType.DNSLookup, dnsRecord);

                //TODO: [Receive and print DNSLookupReply from server]

                receivedMessage = ReceiveMessage(clientSocket, ref serverEndPoint);

                switch (receivedMessage.MsgType)
                {
                    //TODO: [Send Acknowledgment to Server]
                    // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

                    case MessageType.DNSLookupReply:
                        Console.WriteLine("Received 'DNSLookupReply' from server.");
                        Console.WriteLine();
                        int randomMsgId = new Random().Next(1, 10000);
                        SendMessage(clientSocket, serverEndPoint, randomMsgId, MessageType.Ack, receivedMessage.MsgId);
                        msgIdCounter++;
                        break;

                    case MessageType.Error:
                        Console.WriteLine("Received 'Error' message from server.");
                        Console.WriteLine();
                        msgIdCounter++;
                        break;

                    default:
                        Console.WriteLine($"Received unexpected message type: {receivedMessage.MsgType}");
                        Console.WriteLine();
                        break;
                }
            }
            receivedMessage = ReceiveMessage(clientSocket, ref clientEndPoint);

            //TODO: [Receive and print End from server]

            if (receivedMessage?.MsgType == MessageType.End)
            {
                Console.WriteLine("Received 'End' message from server");
                Console.WriteLine();
                return;
            }
            else
            {
                Console.WriteLine("Did not receive 'End' message from server");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("Did not receive 'Welcome' message from server.");
            Console.WriteLine();
        }
    }

    public static void SendMessage(Socket clientSocket, EndPoint serverEndPoint, int msgId, MessageType msgType, object? content)
    {
        Message message = new Message
        {
            MsgId = msgId,
            MsgType = msgType,
            Content = content
        };

        string messageJson = JsonSerializer.Serialize(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

        clientSocket.SendTo(messageBytes, serverEndPoint);

        Console.WriteLine($"Sent '{msgType}' message to the server: {messageJson}");
        Console.WriteLine();
    }

    public static Message? ReceiveMessage(Socket clientSocket, ref IPEndPoint serverEndpoint)
    {
        try
        {
            byte[] buffer = new byte[1024];

            EndPoint serverEndPoint = (EndPoint)serverEndpoint;
            int receivedBytes = clientSocket.ReceiveFrom(buffer, ref serverEndPoint);

            serverEndpoint = (IPEndPoint)serverEndPoint;

            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

            Message? message = JsonSerializer.Deserialize<Message>(receivedMessage);

            if (message == null || message.MsgId == 0 || message.MsgType == null)
            {
                Console.WriteLine("Received an invalid or incomplete message.");
                return null;
            }
            
            Console.WriteLine($"Received {message.MsgType} message from server:");
            Console.WriteLine($"Message ID: {message.MsgId}");
            Console.WriteLine($"Content: {message.Content}");
            Console.WriteLine();

            return message;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving message: {ex.Message}");
            Console.WriteLine();
            return null;
        }
    }
}
