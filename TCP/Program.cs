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

    private static TCPAnswerKeyMsg _answerKeyMsg;
    public static void Main(string[] args)
    {

        server.Start();
        Console.WriteLine("Server started... listening on port 12000");
        SHA256 sHA = SHA256.Create();
        byte[] key = sHA.ComputeHash(Encoding.UTF8.GetBytes("secureKey:d")); // A random key
        _answerKeyMsg = new TCPAnswerKeyMsg() { Key = key };

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
        //deep reference to inner dict.
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
                        Console.WriteLine($"Been requested key from {myInfo.ClientGuid}");
                        myInfo.SendMessage(_answerKeyMsg);
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
                        TCPChatMsg chatMes = MessagePackSerializer.Deserialize<TCPChatMsg>(messageBytes);
                        byte[] decryptedChatMsg = Encryption.Decrypt(chatMes.Cypher_Message, _answerKeyMsg.Key, chatMes.IV);
                        string chatMsg = Encoding.UTF8.GetString(decryptedChatMsg);
                        string sentMes = $"{myInfo.Name}: {chatMsg}";

                        Console.WriteLine($"Revieved = {sentMes}");

                        //connectedClients.SendMessageToAll($"{myInfo.Name}: A msg", myInfo);

                        break;
                    case TCPMessagesTypes.C_RequestListMsg:
                        string listUser = connectedClients.GetNameOfAllAsSingleString().ToString();
                        string users = $"\nOnline users:\n{listUser}\n";
                        TCPListMsg listMsg = new TCPListMsg() { List = users };
                        myInfo.SendMessage(listMsg);

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
}

public class ConnectedClients
{
    Dictionary<Guid, ClientInfo> clientsByGuid = new Dictionary<Guid, ClientInfo>();
    public void AddClient(Guid clientGuid, TcpClient client)
    {
        clientsByGuid.Add(clientGuid, new ClientInfo(client) { ClientGuid = clientGuid });
    }

    public void RemoveClient(Guid clientGuid)
    {
        clientsByGuid[clientGuid].Dispose();
        clientsByGuid.Remove(clientGuid);

    }

    public ClientInfo this[Guid clientGuid]
    {
        get { return clientsByGuid[clientGuid]; }
        set { clientsByGuid[clientGuid] = value; }
    }

    public string GetNameOfAllAsSingleString()
    {
        List<string> names = new List<string>();
        foreach (var clientInfo in clientsByGuid.Values)
        {
            names.Add(clientInfo.Name);
        }
        return string.Join(Environment.NewLine, names);
    }
    public void SendMessageToAll(string message, ClientInfo info = null)
    {
        foreach (var kvp in clientsByGuid)
        {
            if (info != null && info == kvp.Value) continue;
            kvp.Value.SendMessage(message);
        }
    }

    public void SendMessageToAll(TCPNetworkMessage message)
    {
        foreach (var kvp in clientsByGuid)
        {
            kvp.Value.SendMessage(message);
        }
    }

    public void SendDirectMessage(string message, string recipient, Guid sender)
    {
        var foundClient = clientsByGuid.Values.FirstOrDefault(clientInfo => clientInfo.Name == recipient);
        if (foundClient != null)
        {
            foundClient.SendMessage(message);
        }
        else
        {
            clientsByGuid[sender].SendMessage(recipient + " is not online!");
        }

    }

}

public class ClientInfo
{
    public Guid ClientGuid { get; set; }
    public TcpClient client;
    public string Name { get; set; } = string.Empty;
    BinaryWriter bWriter;
    public ClientInfo(TcpClient client)
    {
        bWriter = new BinaryWriter(client.GetStream());
        this.client = client;
    }
    public void SendMessage(string message)
    {
        byte[] messageBytes = new byte[1024];
        TCPServerMsg mes = new TCPServerMsg() { Message = message };
        messageBytes = MessagePackSerializer.Serialize(mes);

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
                messageBytes = MessagePackSerializer.Serialize((TCPChatMsg)message);
                break;
            case TCPMessagesTypes.S_WelcomeNewUser:
                messageBytes = MessagePackSerializer.Serialize((TCPWelcomeMsg)message);
                break;
            case TCPMessagesTypes.S_ServerMessage:
                messageBytes = MessagePackSerializer.Serialize((TCPServerMsg)message);
                break;
            case TCPMessagesTypes.S_ListMsg:
                messageBytes = MessagePackSerializer.Serialize((TCPListMsg)message);
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

public class PostRestData
{
    public PostRestData(string name, string mess)
    {
        this.Message = mess;
        this.Name = name;
    }

    public string Message { get; }
    public string Name { get; }
}


    //static void HandleDirectMessage(DirectMessage dMes, Guid sender)
    //{
    //    connectedClients.SendDirectMessage("Direct: " + connectedClients[sender].Name + " : " + dMes.Message, dMes.recipient, sender);
    //}

    //static void HandleGlobelMessage(GlobalMessage mes, Guid sender)
    //{
    //    connectedClients.SendMessageToAll(connectedClients[sender].Name + " : " + mes.Message);
    //}

//internal class Program
//{
//    static List<ClientData> clients = new List<ClientData>();
//    static byte[] _key;
//    static void Main(string[] args)
//    {
//        TcpListener server = new TcpListener(IPAddress.Any, 12345);
//        server.Start();
//        Console.WriteLine("Awaiting client connection");

//        SHA256 sHA = SHA256.Create();
//        _key = sHA.ComputeHash(Encoding.UTF8.GetBytes("ImportantKey")); // A random key

//        while (true)
//        {
//            TcpClient client = server.AcceptTcpClient();
//            Thread clientThread = new Thread(() => HandleClient(client));
//            clientThread.IsBackground = true;
//            clientThread.Start();
//        }
//    }

//    static void HandleClient(TcpClient client)
//    {
//        Guid clientId = Guid.NewGuid();
//        Console.WriteLine($"{clientId} has connected.");
//        ClientData cliData = new ClientData(clientId, "", client);
//        clients.Add(cliData);

//        bool firstMess = true;
//        //Sends the chosen client name to the client.
//        //Receives and sends messages via the following.
//        StreamReader reader = new StreamReader(client.GetStream());
//        try
//        {
//            while (client.Connected)
//            {
//                string message = reader.ReadLine();
//                if (message != null)
//                {
//                    if (firstMess)
//                    {
//                        cliData.name = message;
//                        RelayMessage(clientId, "server ", $"{cliData.name} has joined.");
//                        firstMess = false;
//                    }
//                    else
//                    {
//                        if (message == "!List")
//                        {
//                            MessageSelf(cliData);
//                        }
//                        else if (message == "!Log")
//                        {
//                            GetMessage(cliData);

//                        }
//                        else
//                        {
//                            Console.WriteLine($"Received message from {clientId}: {message}");
//                            PostMessage(cliData.name, message);
//                            RelayMessage(clientId, cliData.name, message);
//                        }
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {

//            Console.WriteLine($"An exception has occured for client {clientId}: {ex.Message}");
//        }
//        finally
//        {
//            Console.WriteLine($"{clientId} has disconnected.");

//            var searchedClient = clients.First(x => x.tcpClient == client);
//            clients.Remove(searchedClient);
//            client.Dispose();
//            RelayMessage(clientId, "server ", $"{cliData.name} has abandoned the cause.");
//        }
//    }

//    static void RelayMessage(Guid clientID, string name, string mess)
//    {
//        for (int i = 0; i < clients.Count; i++)
//        {

//            if (clientID != clients[i].id)
//            {
//                StreamWriter writer = new StreamWriter(clients[i].tcpClient.GetStream());
//                writer.Flush();
//                writer.WriteLine(name + ": " + mess);
//                writer.Flush();
//            }
//        }
//    }

//    static void MessageSelf(ClientData requester)
//    {
//        StreamWriter author = new StreamWriter(requester.tcpClient.GetStream());
//        author.Flush();
//        author.WriteLine("Participants :");
//        for (int i = 0; i < clients.Count; i++)
//        {
//            author.WriteLine(" -" + clients[i].name+".");
//        }
//        author.Flush();
//    }

//    // Poster name and message through REST to log
//    static async void PostMessage(string name, string mess)
//    {
//        HttpClient httpClient = new HttpClient();

//        PostRestData test = new PostRestData(name, mess);
//        var jsonData = JsonSerializer.Serialize(test);

//        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
//        HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

//        if (response.IsSuccessStatusCode)
//        {
//            // Håndter succesresponsen
//            string responseContent = await response.Content.ReadAsStringAsync();
//            Console.WriteLine($"POST-anmodning lykkedes. Respons: {responseContent}");
//        }
//        else
//        {
//            // Håndter fejlresponsen (statuskode er ikke 2xx)
//            Console.WriteLine($"POST-anmodning mislykkedes. Statuskode: {response.StatusCode}");
//        }

//    }

//    static string apiUrl = "https://localhost:7019/LogChat";

//    // Through REST GET the message to client
//    static async void GetMessage(ClientData requester)
//    {
//        HttpClient httpClient = new HttpClient();
//        HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

//        if (response.IsSuccessStatusCode)
//        {

//            string responseContent = await response.Content.ReadAsStringAsync();

//            StreamWriter writer = new StreamWriter(requester.tcpClient.GetStream());

//            writer.WriteLine($"Server Log: \n {responseContent}" );
//            writer.Flush();

//            Console.WriteLine($"Log Data send to: /*{requester.name}*/: {responseContent}");
//        }
//        else
//        {

//            Console.WriteLine($"Anmodningen mislykkedes. Statuskode: {response.StatusCode}");
//        }
//;

//    }
//}