using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace locationserver
{
    class Program
    {
        static ConcurrentDictionary<string, string> database = new ConcurrentDictionary<string, string>(); 

        static void Main(string[] args)
        {
            database.GetOrAdd("DBate", "at home");
            database.GetOrAdd("602854", "at home");
            database.GetOrAdd("cssbct", "at home");
            runServer();
        }
        static void runServer()
        {
            TcpListener listener;
            Socket connection;
            Request request;
            try
            {
                listener = new TcpListener(IPAddress.Any, 43);
                listener.Start();
                Console.WriteLine("Listening...");
                while (true)
                {          
                    connection = listener.AcceptSocket();
                    request = new Request();
                    Thread thread = new Thread(() => request.doRequest(connection));
                    thread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        class Request
        {
            public void doRequest(Socket pConnection)
            {
                NetworkStream networkStream = new NetworkStream(pConnection);
                StreamWriter sw = new StreamWriter(networkStream);
                StreamReader sr = new StreamReader(networkStream);
                try
                {   
                    string connectedIP = pConnection.RemoteEndPoint.ToString();
                    Console.WriteLine("Listener accepted a connection from " + connectedIP);

                    pConnection.ReceiveTimeout = 1000;
                    
                    string line = sr.ReadLine();
                    Console.WriteLine("Response received: " + line);                    


                    string name;
                    string location;
                    string response = null;
                    string[] split;
                    
                    pConnection.SendTimeout = 1000;

                    if (line.StartsWith("GET /?") && line.Contains('='))
                    {
                        split = line.Split('=', ' ');
                        name = split[2];

                        if (database.TryGetValue(name, out location))
                        {
                            response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n" + location;
                        }
                        else
                        {
                            response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n";
                        }
                    } //HTTP 1.1 Lookup
                    else if (line.StartsWith("GET /?"))
                    {
                        split = line.Split('?', ' ');
                        name = split[2];

                        if (database.TryGetValue(name, out location))
                        {
                            response = "HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n" + location;
                        }
                        else
                        {
                            response = "HTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n";
                        }
                    } //HTTP 1.0 Lookup
                    else if (line.StartsWith("GET /"))
                    {
                        split = line.Split('/');
                        name = split[1];

                        if (database.TryGetValue(name, out location))
                        {
                            response = "HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n" + location;
                        }
                        else
                        {
                            response = "HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n";
                        }
                    } //HTTP 0.9 Lookup
                    else if (!line.Contains(' '))
                    {
                        name = line;

                        if (database.TryGetValue(name, out location))
                        {
                            response = location;
                        }
                        else
                        {
                            response = "ERROR: no entries found";
                        }
                    } //whois Lookup
                    else if (line.Equals("POST / HTTP/1.1"))
                    {
                        while (!line.StartsWith("name="))
                        {
                            line = sr.ReadLine();
                        }
                        split = line.Split('=', '£');
                        name = split[1];
                        location = split[3];

                        if (database.ContainsKey(name))
                        {
                            database[name] = location;
                            response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n";
                        }
                        else
                        {
                            response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n";
                        }                        
                    } //HTTP 1.1 Update
                    else if (line.StartsWith("POST /") && line.EndsWith(" HTTP/1.0"))
                    {
                        split = line.Split('/', ' ');
                        name = split[2];

                        line = sr.ReadLine();
                        split = line.Split(' ');
                        int contentLength = int.Parse(split[1]);

                        do
                        {
                            line = sr.ReadLine();
                        } while (line.Length != contentLength);

                        location = line;

                        if (database.ContainsKey(name))
                        {
                            database[name] = location;
                        }
                        else
                        {
                            database.GetOrAdd(name, location);
                        }
                        response = "HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n";
                    } //HTTP 1.0 Update
                    else if (line.StartsWith("PUT /"))
                    {
                        split = line.Split('/');
                        name = split[1];
                        for (int i = 0; i < 2; i++)
                        {
                            line = sr.ReadLine();
                        }
                        location = line;

                        if (database.ContainsKey(name))
                        {
                            database[name] = location;
                        }
                        else
                        {
                            database.GetOrAdd(name, location);
                        }
                        response = "HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n";
                    } //HTTP 0.9 Update
                    else if (line.Contains(' '))
                    {
                        split = line.Split(new char[] { ' ' }, 2);
                        name = split[0];
                        location = split[1];

                        if (database.ContainsKey(name))
                        {
                            database[name] = location;
                        }
                        else
                        {
                            database.GetOrAdd(name, location);
                        }
                        response = "OK";
                    } //whois Update

                    Console.WriteLine("Response sent:\r\n " + response);
                    sw.WriteLine(response);
                    sw.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An unexpected error has occured: " + e.ToString());
                    sw.WriteLine("An unexpected error has occured: " + e.ToString());
                    sw.Flush();
                }

                networkStream.Close();
                pConnection.Close();
            }
        }
    }
}
