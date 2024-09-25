using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using MessagePack;
using System.Collections;

namespace TCP;

public static class Program
{
    private static ConnectedClients connectedClients = new ConnectedClients();
    private static TcpListener server = new TcpListener(IPAddress.Any, 12000);
    static string apiUrl = "https://localhost:7019/LogChat";

    public static void Main(string[] args)
    {
        Console.WriteLine("I'm the TCP Chat server\n");

        server.Start();
        Console.WriteLine("Server started... listening on port 12000");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        BinaryReader bwr = new BinaryReader(client.GetStream());

        Guid clientGuid = Guid.NewGuid();
        connectedClients.AddClient(clientGuid, client);
        
        ClientInfo myInfo = connectedClients[clientGuid];
        try
        {
            while (client.Connected)
            {
                int messageLength = bwr.ReadInt32();
                byte messageType = bwr.ReadByte();
                // Read the message data
                byte[] messageBytes = bwr.ReadBytes(messageLength);
                TCPMessagesTypes recievedType = (TCPMessagesTypes)messageType;

                switch (recievedType)
                {
                    case TCPMessagesTypes.C_RequestKey:
                        TCPRequestKeyMsg requestKeyMsg = MessagePackSerializer.Deserialize<TCPRequestKeyMsg>(messageBytes);
                        myInfo.ClientKey = requestKeyMsg.ClientKey;
                        Console.WriteLine($"Recived client key {requestKeyMsg.ClientKey} from {myInfo.ClientGuid}");
                        Console.WriteLine($"Been requested server key from {myInfo.ClientGuid}");

                        TCPAnswerKeyMsg answerKeyMsg = new TCPAnswerKeyMsg() { ServerKey = myInfo.ServerKey};
                        myInfo.SendMessage(answerKeyMsg);
                        continue;
                }

                if ((myInfo.Name == string.Empty) && recievedType != TCPMessagesTypes.C_JoinServer)
                {
                    //Oh no error? break return?
                    myInfo.SendMessage("SERVER: You dont have a user name set!");
                    continue;
                }

                switch (recievedType)
                {
                    case TCPMessagesTypes.C_JoinServer:
                        TCPJoinServerMsg joinServer = MessagePackSerializer.Deserialize<TCPJoinServerMsg>(messageBytes);
                        myInfo.Name = joinServer.Name;
                        string welcomeMsg = $"New user {joinServer.Name} joined";
                        Console.WriteLine(welcomeMsg);

                        connectedClients.SendMessageToAll(welcomeMsg + ": Say hallo:D", myInfo);
                        break;
                    case TCPMessagesTypes.ChatMessage:
                        TCPChatMsg chatMes = Encryption.DeSerilizeChatMsg(messageBytes, myInfo.ClientKey);
                        string sentMes = $"{myInfo.Name}: {chatMes.Temp_Text}";
                        Console.WriteLine($"Revieved = {sentMes}");
                        PostMessage(sentMes);
                        connectedClients.SendMessageToAll(sentMes, myInfo);
                        break;

                    case TCPMessagesTypes.C_RequestListMsg:
                        string listUser = connectedClients.GetNameOfAllAsSingleString().ToString();
                        string users = $"\nOnline users:\n{listUser}\n";
                        myInfo.SendMessage(users);
                        break;

                    case TCPMessagesTypes.C_RequestLogMsg:
                        GetMessage(myInfo); // gets the log and sends to the user
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred for client {myInfo.Name}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Client disconnected: {myInfo.Name}");
            connectedClients.RemoveClient(clientGuid);
            connectedClients.SendMessageToAll(myInfo.Name + " left the server...");
        }
    }

    // Poster name and message through REST to log
    static async void PostMessage(string message)
    {
        HttpClient httpClient = new HttpClient();

        var jsonData = JsonSerializer.Serialize(message);

        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            // Håndter succesresponsen
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"POST-anmodning lykkedes. Respons: {responseContent}");
        }
        else
        {
            // Håndter fejlresponsen (statuskode er ikke 2xx)
            Console.WriteLine($"POST-anmodning mislykkedes. Statuskode: {response.StatusCode}");
        }
    }

    // Through REST GET the message to client
    static async void GetMessage(ClientInfo requester)
    {
        HttpClient httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

        // What if its okay but there is nothing inside it? Another msg to user?
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();

            requester.SendMessage($"\nServer Log:\n{responseContent}");
            Console.WriteLine($"Log Data send to: /*{requester.Name}*/: {responseContent}");
        }
        else
        {
            Console.WriteLine($"Anmodningen mislykkedes. Statuskode: {response.StatusCode}");
        }
    }
}

public class ConnectedClients
{
    private Dictionary<Guid, ClientInfo> _clientsByGuid = new Dictionary<Guid, ClientInfo>();
    
    public void AddClient(Guid clientGuid, TcpClient client)
    {
        _clientsByGuid.Add(clientGuid, new ClientInfo(client) { ClientGuid = clientGuid });
    }

    public void RemoveClient(Guid clientGuid)
    {
        _clientsByGuid[clientGuid].Dispose();
        _clientsByGuid.Remove(clientGuid);
    }

    public ClientInfo this[Guid clientGuid]
    {
        get { return _clientsByGuid[clientGuid]; }
        set { _clientsByGuid[clientGuid] = value; }
    }

    public string GetNameOfAllAsSingleString()
    {
        List<string> names = new List<string>();
        foreach (var clientInfo in _clientsByGuid.Values)
        {
            names.Add(clientInfo.Name);
        }
        return string.Join(Environment.NewLine, names);
    }
    
    public void SendMessageToAll(string message, ClientInfo info = null)
    {
        foreach (var kvp in _clientsByGuid)
        {
            if (info != null && info == kvp.Value) continue;
            kvp.Value.SendMessage(message);
        }
    }

    public void SendMessageToAll(TCPNetworkMessage message)
    {
        foreach (var kvp in _clientsByGuid)
        {
            kvp.Value.SendMessage(message);
        }
    }

    public void SendDirectMessage(string message, string recipient, Guid sender)
    {
        var foundClient = _clientsByGuid.Values.FirstOrDefault(clientInfo => clientInfo.Name == recipient);
        if (foundClient != null)
        {
            foundClient.SendMessage(message);
        }
        else
        {
            _clientsByGuid[sender].SendMessage(recipient + " is not online!");
        }
    }
}

public class ClientInfo
{
    public Guid ClientGuid { get; set; }
    public TcpClient client;
    public string Name { get; set; } = string.Empty;
    public byte[] ServerKey {  get; set; }
    public byte[] ClientKey {  get; set; }
    BinaryWriter bWriter;
    public ClientInfo(TcpClient client)
    {
        bWriter = new BinaryWriter(client.GetStream());
        ServerKey = Encryption.GetRandomKey();
        this.client = client;
    }
    public void SendMessage(string message)
    {
        byte[] messageBytes = new byte[1024];
        TCPChatMsg mes = new TCPChatMsg() { Temp_Text = message };

        messageBytes = Encryption.SerilizeChatMsg(mes, ServerKey);

        bWriter.Write(messageBytes.Length);
        bWriter.Write(mes.GetMessageTypeAsByte);
        bWriter.Write(messageBytes);
        bWriter.Flush();
    }

    public void SendMessage(TCPNetworkMessage message)
    {
        byte[] messageBytes = new byte[1024];

        switch (message.MessageType)
        {
            case TCPMessagesTypes.S_AnswerKey:
                messageBytes = MessagePackSerializer.Serialize((TCPAnswerKeyMsg)message);
                break;
            case TCPMessagesTypes.ChatMessage:
                messageBytes = Encryption.SerilizeChatMsg((TCPChatMsg)message, ServerKey);
                break;
            case TCPMessagesTypes.S_WelcomeNewUser:
                messageBytes = MessagePackSerializer.Serialize((TCPWelcomeMsg)message);
                break;
            case TCPMessagesTypes.S_ServerMessage:
                messageBytes = MessagePackSerializer.Serialize((TCPServerMsg)message);
                break;
            default:
                return;
        }

        bWriter.Write(messageBytes.Length);
        bWriter.Write(message.GetMessageTypeAsByte);
        bWriter.Write(messageBytes);
        bWriter.Flush();
    }

    public void Dispose()
    {
        bWriter.Dispose();
        client.Dispose();
    }
}


//static void HandleDirectMessage(DirectMessage dMes, Guid sender)
//{
//    connectedClients.SendDirectMessage("Direct: " + connectedClients[sender].Name + " : " + dMes.Message, dMes.recipient, sender);
//}

//static void HandleGlobelMessage(GlobalMessage mes, Guid sender)
//{
//    connectedClients.SendMessageToAll(connectedClients[sender].Name + " : " + mes.Message);
//}
