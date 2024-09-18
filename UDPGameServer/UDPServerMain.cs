
using System.Net.Sockets;
using System.Net;
using System.Text;
using MessagePack;

namespace UDPGameServer;

public class ClientData
{
    public Guid ClientID {  get; set; } 
    public IPEndPoint IPEndPoint { get; set; }
    public DateTime LastHeartBeat { get; set; }
}

public static class UDPServerMain
{
    static int _gridX = 5;
    static int _gridY = 5;
    static int _maxAmountOfPlayers = 2;
    static Grid _gameGrid;


    static UdpClient udpServer = new UdpClient(1234);
    static Dictionary<int, ClientData> Clients;
    private static Random _rnd = new();
    private static int _turnNmb;
    private static ClientData _currentTurnClientData;

    static RequestMovePosMsg? _requestMovePosMsg;
    static readonly object _clientLock = new object();
    static readonly object _moveRequestLock = new object();

    static float TargetFPS = 60; // 

    public static void Main(string[] args)
    {
        Clients = new();
        // Thread that deletes clients if they have lost connection
        Thread heartBeatDeleteWhenOffline = new Thread(HeartBeatDeleteWhenOffline);
        heartBeatDeleteWhenOffline.IsBackground = true;
        heartBeatDeleteWhenOffline.Start();

        // Gameloop only starts when it has enough clients
        Thread gameLoop = new Thread(GameUpdate);
        gameLoop.IsBackground = true;
        gameLoop.Start();

        ReceiveMessages();
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
                    case MessageType.RequestAddClient:
                        // Add endpoint to a List
                        if (!Clients.Any(c => c.Value.IPEndPoint.Equals(remoteEndPoint)) && Clients.Count < _maxAmountOfPlayers)
                        {
                            ClientData data = new ClientData { IPEndPoint = remoteEndPoint, LastHeartBeat = DateTime.Now, ClientID = Guid.NewGuid() };
                            Clients.Add(Clients.Count, data);
                            string welcome = $"NEW CLIENT: {remoteEndPoint}:: ID: {data.ClientID}";
                            Console.WriteLine(welcome);
                            
                            ServerMsg serverMsgWelcom = new ServerMsg() { Message = "Welcome to " + welcome};
                            RelayMessageToAllUser(serverMsgWelcom);
                        }
                        break;

                    case MessageType.HeartBeat:
                        // Check if the client is in the list
                        var client = Clients.FirstOrDefault(c => c.Value.IPEndPoint.Equals(remoteEndPoint));

                        if (client.Equals(default(KeyValuePair<int, ClientData>)))
                        {
                            Console.WriteLine($"HeartBeat ERROR: Client {remoteEndPoint} not found in the list.");
                            break;
                        }

                        int clientIndex = client.Key;
                        Clients[clientIndex].LastHeartBeat = DateTime.Now;
                        break;

                    case MessageType.RequestMovePosition:

                        var senderClient = Clients.FirstOrDefault(c => c.Value.IPEndPoint.Equals(remoteEndPoint));
                        if (senderClient.Equals(default(KeyValuePair<int, ClientData>)))
                        {
                            Console.WriteLine($"RequestMovePosition ERROR: Client {remoteEndPoint} not found in the list.");
                            break;
                        }

                        lock (_moveRequestLock)
                        {
                            if (senderClient.Value.ClientID == _currentTurnClientData.ClientID)
                            {
                                // Only take the request if its their turn
                                _requestMovePosMsg = MessagePackSerializer.Deserialize<RequestMovePosMsg>(dataToDeserialize);
                            }
                        }
                        break;

                    case MessageType.ServerMsg:
                        ServerMsg serverMsg = MessagePackSerializer.Deserialize<ServerMsg>(dataToDeserialize);
                        Console.WriteLine($"From {remoteEndPoint}: {serverMsg.Message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive Exception: {ex.Message}");
            }
        }
    }


    // Ask for turns
    // Give aceess so the first client can change pos
    // Say to the other client whos turn it is. 
    // Wait for answer

    static void GameUpdate()
    {
        bool first = false;
        while (true)
        {
            Thread.Sleep(50);
            // Take time for it to be done, then turn sleep time up or down.

            //lock (_clientLock)
            //{
            if (Clients.Count != _maxAmountOfPlayers) continue;
            if (!first)
            {
                first = true;
                _turnNmb = 0;
                _currentTurnClientData = Clients[_turnNmb];
                StartGame();
                continue;
            }

            bool haveMoved = false;
            
            while (!haveMoved)
            {
                lock (_moveRequestLock)
                {
                    if (_requestMovePosMsg == null) continue;

                    // Check the move data
                    // If the msg is not a valid pos, continue;
                    // Handle dmg and updated grid.
                    // Send new data before change turn.
                    // client has to wait for the updated grid, before its their turn.

                    Point prevPos = _requestMovePosMsg.PrevPos;
                    Point newTargetPoint = _requestMovePosMsg.NewTargetPos;

                    TryMoveData tryMoveData = _gameGrid.TryMoveObject(prevPos, newTargetPoint);

                    if (tryMoveData.HasMoved)
                    {
                        Console.WriteLine($"User {_turnNmb}: {tryMoveData.ReturnMsg}");
                        haveMoved = true;
                    }

                    ServerMsg serverMsg = new ServerMsg() { Message = tryMoveData.ReturnMsg };
                    SendMessage(serverMsg, _currentTurnClientData.IPEndPoint);
                    _requestMovePosMsg = null;
                }
            }

            ChangeTurn();
        }
        //}
    }

    static byte[] SendMessage(NetworkMessage message, IPEndPoint endPoint)
    {
        byte[] messageBytes = new byte[1024];
        byte messageTypeByte = message.GetMessageTypeAsByte;

        switch (message.MessageType)
        {
            case MessageType.StartGame:
                messageBytes = MessagePackSerializer.Serialize((StartGameMsg)message);
                break;
            case MessageType.UpdateGrid:
                messageBytes = MessagePackSerializer.Serialize((UpdateGridMsg)message);
                break;
            case MessageType.TurnMsg:
                messageBytes = MessagePackSerializer.Serialize((TurnMsg)message);
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

        return combinedBytes;
    }

    static void SendRepeatMessage(byte[] combinedBytes, IPEndPoint endPoint)
    {
        udpServer.Send(combinedBytes, endPoint);
    }

    static void RelayMessageToAllUser(NetworkMessage message)
    {
        byte[] combinedMsg = null;
        for (int i = 0; i < Clients.Count; i++)
        {
            IPEndPoint _clientEndPoint = Clients[i].IPEndPoint;
            if (combinedMsg == null)
                combinedMsg = SendMessage(message, _clientEndPoint);
            else
                SendRepeatMessage(combinedMsg, _clientEndPoint);
        }
    }

    static void StartGame()
    {
        //Console.Clear();
        Console.WriteLine($"Started game with {Clients.Count} players");

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

                Character player = new Character(i, new Point(x, y), $"Bob {i}", CharacterType.Warrior, 5, 10);
                _gameGrid.AddObject(player, x, y);
            }
        }

        UpdateGrid();
    }


    static void UpdateGrid()
    {
        SendTurnMsg();

        UpdateGridMsg updateGrid = new UpdateGridMsg() { GameGridArray = _gameGrid.CharacterGrid, GridSize = _gameGrid.GridSize };

        RelayMessageToAllUser(updateGrid);
    }

    static void SendTurnMsg()
    {
        //Make a base response for the current turn user, and otherwise its a waiting turn.
        string othersResponseMessage = $"Its {_turnNmb} turn...";
        byte[] othersResponseData = Encoding.ASCII.GetBytes(othersResponseMessage);

        string responseMessage = $"Its your turn...";
        byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);

        TurnMsg turnMsg;
        for (int i = 0; i < Clients.Count; i++)
        {
            if (i == _turnNmb)
                turnMsg = new TurnMsg() { Message = responseMessage };
            else
                turnMsg = new TurnMsg() { Message = othersResponseMessage };

            SendMessage(turnMsg, Clients[i].IPEndPoint);
        }
    }

    static void ChangeTurn()
    {
        _turnNmb = (_turnNmb + 1) % _maxAmountOfPlayers;
        _currentTurnClientData = Clients[_turnNmb];
        UpdateGrid();
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
}