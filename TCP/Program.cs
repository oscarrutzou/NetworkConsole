using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Text;
namespace TCP
{
    internal class Program
    {
        
        static List<ClientData> clients = new List<ClientData>();
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 12345);
            server.Start();
            Console.WriteLine("Awaiting client connection");
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
                            else if (message == "!Log")
                            {
                                GetMessage(cliData);
                                                            
                            }
                            else
                            {
                                Console.WriteLine($"Received message from {clientId}: {message}");
                                PostMessage(cliData.name, message);
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

                var searchedClient = clients.First(x => x.tcpClient == client);
                clients.Remove(searchedClient);
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

        // Poster name and message through REST to log
        static async void PostMessage(string name, string mess)
        {
            HttpClient httpClient = new HttpClient();

            PostTest test = new PostTest(name, mess);
            var jsonData = JsonSerializer.Serialize(test);

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

        static string apiUrl = "https://localhost:7019/LogChat";

        // Through REST GET the message to client
        static async void GetMessage(ClientData requester)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                
                string responseContent = await response.Content.ReadAsStringAsync();

                StreamWriter writer = new StreamWriter(requester.tcpClient.GetStream());
                
                writer.WriteLine($"Server Log: \n {responseContent}" );
                writer.Flush();
                
                Console.WriteLine($"Log Data send to: /*{requester.name}*/: {responseContent}");
            }
            else
            {
                
                Console.WriteLine($"Anmodningen mislykkedes. Statuskode: {response.StatusCode}");
            }
    ;

        }
    }

    public class PostTest
    {
        public string msg;
        public PostTest(string name, string mess)
        {
            this.mess = mess;

            this.name = name;

        }

        public string mess { get; }
        public Guid clientID { get; }
        public string name { get; }
    }


}

