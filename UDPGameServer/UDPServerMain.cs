
using System.Net.Sockets;
using System.Net;
using System.Text;
using MessagePack;

namespace UDPGameServer;

public class ClientData
{
    public IPEndPoint IPEndPoint { get; set; }
    public DateTime LastHeartBeat { get; set; }
}

public static class UDPServerMain
{
    static int _gridX = 5;
    static int _gridY = 5;
    static int _maxAmountOfPlayers = 1;
    static Grid _gameGrid;


    static UdpClient udpServer = new UdpClient(1234);
    static Dictionary<int, ClientData> Clients = new();
    private static Random _rnd = new();
    private static int _turnNmb;

    public static void Main(string[] args)
    {
        // Thread that deletes clients if they have lost connection
        Thread heartBeatDeleteWhenOffline = new Thread(HeartBeatDeleteWhenOffline);
        heartBeatDeleteWhenOffline.IsBackground = true;
        heartBeatDeleteWhenOffline.Start();

        // Gameloop only starts when it has enough clients
        Thread gameLoop = new Thread(GameUpdate);
        gameLoop.IsBackground = true;
        gameLoop.Start();

        HeartBeatListener();
    }

    /// <summary>
    /// Checks the heart beat and adds new clients to a dictionary
    /// </summary>
    static void HeartBeatListener()
    {
        while (true)
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receivedData = udpServer.Receive(ref clientEndPoint);
            // Take a message insted...
            string receivedMessage = Encoding.ASCII.GetString(receivedData);

            // Add endpoint to a List
            if (!Clients.Any(c => c.Value.IPEndPoint.Equals(clientEndPoint)) && Clients.Count < _maxAmountOfPlayers)
            {
                Clients.Add(Clients.Count, new ClientData { IPEndPoint = clientEndPoint, LastHeartBeat = DateTime.Now });
                Console.WriteLine($"NEW: {clientEndPoint} joined");
            }
            
            // Check if the client is in the list
            var client = Clients.FirstOrDefault(c => c.Value.IPEndPoint.Equals(clientEndPoint));
            
            if (client.Equals(default(KeyValuePair<int, ClientData>)))
            {
                Console.WriteLine($"ERROR: Client {clientEndPoint} not found in the list.");
                continue;
            }

            int clientIndex = client.Key;

            if (receivedMessage == HeartBeatMsg)
            {
                // Update the LastHeartBeat time
                Clients[clientIndex].LastHeartBeat = DateTime.Now;
                continue;
            }
        }
    }

    static void ReceiveMessages()
    {
        while (true)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpServer.Receive(ref remoteEndPoint);
                MessageType messageType = (MessageType)receivedData[0];
                byte[] dataToDeserialize = receivedData.Skip(1).ToArray();

                switch (messageType)
                {
                    case MessageType.StartGame:
                        break;

                    case MessageType.HeartBeat:
                        break;

                    case MessageType.MovePosition:
                        break;

                    case MessageType.ServerMsg:
                        ServerMsg serverMsg = MessagePackSerializer.Deserialize<ServerMsg>(dataToDeserialize);
                        Console.WriteLine($"From {remoteEndPoint}: {serverMsg.Message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Receive Exception: {ex.Message}");
            }
        }
    }

    public static readonly string HeartBeatMsg = "heartbeat";
    static object _clientLock = new object();
    // Ask for turns
    // Give aceess so the first client can change pos
    // Say to the other client whos turn it is. 
    // Wait for answer

    static void GameUpdate()
    {
        bool first = false;
        while (true)
        {
            lock (_clientLock)
            {
                if (Clients.Count != _maxAmountOfPlayers) continue;
                if (!first)
                {
                    first = true;
                    _turnNmb = 0;
                    StartGame();
                    continue;
                }

                //Make a base response for the current turn user, and otherwise its a waiting turn.

                string othersResponseMessage = $"Its {_turnNmb} turn...";
                byte[] othersResponseData = Encoding.ASCII.GetBytes(othersResponseMessage);

                string responseMessage = $"Its your turn...";
                byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);

                ServerMsg serverMsg;
                for (int i = 0; i < Clients.Count; i++)
                {
                    if (i == _turnNmb)
                        serverMsg = new ServerMsg() { Message = responseMessage };
                    else
                        serverMsg = new ServerMsg() { Message = othersResponseMessage };
                    
                    SendMessage(serverMsg, Clients[i].IPEndPoint);
                }

                // Uses a temporary variable for the IPEndPoint
                IPEndPoint tempEndPoint = Clients[_turnNmb].IPEndPoint;
                
                bool hasRecivedNewPos = false;
                while (!hasRecivedNewPos)
                {
                    byte[] receivedData = udpServer.Receive(ref tempEndPoint);
                    string receivedMessage = Encoding.ASCII.GetString(receivedData);

                    if (receivedMessage == HeartBeatMsg) continue;
                    // If the msg is not a valid pos, continue;

                    Console.WriteLine($"Svar fra serveren: {receivedMessage} på adresse: {Clients[_turnNmb].IPEndPoint}");
                    hasRecivedNewPos = true;
                }

                //ChangeTurn();
            }
        }
    }
    static void SendMessage(NetworkMessage message, IPEndPoint endPoint)
    {
        byte[] messageBytes = new byte[1024];
        byte messageTypeByte = message.GetMessageTypeAsByte;

        switch (message.MessageType)
        {
            case MessageType.StartGame:
                messageBytes = MessagePackSerializer.Serialize((StartGame)message);
                break;
            case MessageType.ServerMsg:
                messageBytes = MessagePackSerializer.Serialize((ServerMsg)message);
                break;
            default:
                break;
        }

        byte[] combinedBytes = new byte[1 + messageBytes.Length];
        combinedBytes[0] = messageTypeByte;
        Buffer.BlockCopy(messageBytes, 0, combinedBytes, 1, messageBytes.Length);
        udpServer.Send(combinedBytes, endPoint);
    }

    static void StartGame()
    {
        Console.Clear();
        Console.WriteLine($"Started game with {Clients.Count} players");
        // Make world
        // Sets players
        // Sends full data to players (full grid)

        _gameGrid = new Grid(_gridX, _gridY);

        for (int i = 0; i < Clients.Count; i++)
        {
            bool hasFoundSpot = false;

            while (!hasFoundSpot)
            {
                int x = _rnd.Next(0, _gridX);
                int y = _rnd.Next(0, _gridY);
                if (_gameGrid.CharacterGrid[x, y] != null) continue;
                hasFoundSpot = true;

                Character player = new Character(i, new Point(x, y), $"Bob {i}", CharacterType.Warrior, 10, 5);
                _gameGrid.AddObject(player, x, y);
            }
        }

        _gameGrid.DrawGrid();
    }

    static void ChangeTurn()
    {
        _turnNmb = (_turnNmb + 1) % _maxAmountOfPlayers;
    }

    static void HeartBeatDeleteWhenOffline()
    {
        List<ClientData> clientToDelete = new();
        while (true)
        {
            Thread.Sleep(200);
            clientToDelete.Clear();

            // Loops though the list. 
            foreach (ClientData data in Clients.Values)
            {
                TimeSpan timeSpan = DateTime.Now - data.LastHeartBeat;

                if (timeSpan.TotalMilliseconds >= 2000)
                {
                    clientToDelete.Add(data);
                }
            }

            lock (_clientLock)
            {
                foreach (var item in clientToDelete)
                {
                    Console.WriteLine($"Client IP {item.IPEndPoint} has disconnected from server...");
                    int clientIndex = Clients.First(c => c.Value.IPEndPoint.Equals(item.IPEndPoint)).Key;
                    Clients.Remove(clientIndex);
                    StopGame();
                }
            }
        }
    }

    static void StopGame()
    {
        // Removes all clients
        // Stop the game client + chat client.
    }




    // When there is 2 clients we stop reciving clients.
    //Console.WriteLine($"Modtaget: {receivedMessage} fra {clientEndPoint}");



    // Answer from server!
    //byte[] receivedData = _udpClient.Receive(ref _endPoint);
    //string receivedMessage = Encoding.ASCII.GetString(receivedData);
    //Console.WriteLine($"Svar fra serveren: {receivedMessage} på adresse: {_endPoint}");

    //string responseMessage = "Tak for beskeden!";
    //byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
    //udpServer.Send(responseData, clientEndPoint);
}