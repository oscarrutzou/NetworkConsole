
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace UDPGameServer;

public static class UDPServerMain
{
    static int _gridX = 5;
    static int _gridY = 5;
    static int _maxAmountOfPlayers = 2;
    static Grid _grid;


    static UdpClient udpServer = new UdpClient(1234);
    static Dictionary<IPEndPoint, DateTime> clients = new();
    static IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

    public static void Main(string[] args)
    {
        _grid = new Grid(_gridX, _gridY);

        // Thread that handles the listener for the heartbeats
        //Thread heartBeatListener = new Thread(HeartBeatListener);
        //heartBeatListener.IsBackground = true;
        //heartBeatListener.Start();

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
            byte[] receivedData = udpServer.Receive(ref clientEndPoint);
            // Take a message insted...
            string receivedMessage = Encoding.ASCII.GetString(receivedData);

            // Add endpoint to a List
            if (!clients.ContainsKey(clientEndPoint) && clients.Count < _maxAmountOfPlayers)
            {
                clients.Add(clientEndPoint, DateTime.Now);
            }

            if (receivedMessage == "heartbeat")
            {
                // Add the new time
                clients[clientEndPoint] = DateTime.Now;
                continue;
            }
        }
    }

    // When there is 2 clients we stop reciving clients.
    //Console.WriteLine($"Modtaget: {receivedMessage} fra {clientEndPoint}");

    //string responseMessage = "Tak for beskeden!";
    //byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
    //udpServer.Send(responseData, clientEndPoint);
    static void GameUpdate()
    {
        // Ask for turns
        // Give aceess so the first client can change pos
        // Say to the other client whos turn it is. 
        // Wait for answer
        while (true)
        {
            Thread.Sleep(500);
            if (clients.Count != _maxAmountOfPlayers) return;

            Console.WriteLine("Now Awake");
        }
        
    }

    static void HeartBeatDeleteWhenOffline()
    {
        List<IPEndPoint> clientToDelete = new();
        while (true)
        {
            Thread.Sleep(20);
            clientToDelete.Clear();

            // Loops though the list. 
            foreach (IPEndPoint clientEndPoint in clients.Keys)
            {
                TimeSpan timeSpan = DateTime.Now - clients[clientEndPoint];

                if (timeSpan.TotalMilliseconds >= 1000)
                {
                    clientToDelete.Add(clientEndPoint);
                }
            }

            foreach (var item in clientToDelete)
            {
                Console.WriteLine($"Client IP {item} has disconnected from server...");
                clients.Remove(item);
                StopGame();
            }
        }
    }

    static void StopGame()
    {
        // Removes all clients
        // Stop the game client + chat client.
    }
    /*
     *         Character Player = new Character();
        Character enemy = new Character(ObjectType.Enemy, 10, 10, 10);
        _grid.AddObject(Player, 1, 1);
        _grid.AddObject(enemy, 2, 2);
        _grid.AddObject(enemy, 4, 2);
        _grid.DrawGrid();

     */

}