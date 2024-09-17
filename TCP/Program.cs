using System.Net.Sockets;
using System.Net;
namespace TCP
{
    internal class Program
    {
        static List<ClientData> clients = new List<ClientData>();
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 12345);
            server.Start();
            Console.WriteLine("...");
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            Guid clientId = Guid.NewGuid();
            Console.WriteLine($"{clientId} has connected.");
            ClientData cliData = new ClientData(clientId, "", client);
            clients.Add(cliData);
            bool firstMess = true;
            //Sends the chosen client name to the client.
            //Receives and sends messages via the following.
            StreamReader reader = new StreamReader(client.GetStream());
            try
            {
                while (client.Connected)
                {
                    string message = reader.ReadLine();
                    if (message != null)
                    {
                        if (firstMess)
                        {
                            cliData.name = message;
                            RelayMessage(clientId, "server ", $"{cliData.name} has joined.");
                            firstMess = false;
                        }
                        else
                        {
                            if (message == "!List")
                            {
                                MessageSelf(cliData);
                            }
                            else
                            {
                                Console.WriteLine($"Received message from {clientId}: {message}");
                                RelayMessage(clientId, cliData.name, message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An exception has occured for client {clientId}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"{clientId} has disconnected.");

                client.Dispose();
                RelayMessage(clientId, "server ", $"{cliData.name} has abandoned the cause.");
            }
        }

        static void RelayMessage(Guid clientID, string name, string mess)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clientID != clients[i].id)
                {
                    StreamWriter writer = new StreamWriter(clients[i].tcpClient.GetStream());
                    writer.Flush();
                    writer.WriteLine(name + ": " + mess);
                    writer.Flush();
                }
            }
        }

        static void MessageSelf(ClientData requester)
        {
            StreamWriter author = new StreamWriter(requester.tcpClient.GetStream());
            author.Flush();
            author.WriteLine("Participants :");
            for (int i = 0; i < clients.Count; i++)
            {
                author.WriteLine(" -" + clients[i].name+".");
            }
            author.Flush();
        }
    }
}
