using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace location
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string server = "whois.net.dcs.hull.ac.uk";
                string protocol = "whois";
                int port = 43;
                string username = null;
                string location = null;
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-h":
                            server = args[++i];
                            break;
                        case "-h9":
                        case "-h0":
                        case "-h1":
                            protocol = args[i];
                            break;
                        case "-p":
                            port = int.Parse(args[++i]);
                            break;
                        default:
                            if (username == null)
                            {
                                username = args[i];
                            }
                            else if (location == null)
                            {
                                location = args[i];
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Provided argument is invalid " + args[i]);
                            }
                            break;
                    }
                }
                if (username == null)
                {
                    Console.WriteLine("ERROR: No username argument provided");
                }

                sendRequest(server, port, protocol, username, location);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }                
        static void sendRequest(string pServer, int pPort, string pProtocol, string pUsername, string pLocation)
        {                
            TcpClient client = new TcpClient();
            client.Connect(pServer, pPort);
            StreamWriter sw = new StreamWriter(client.GetStream());
            StreamReader sr = new StreamReader(client.GetStream());
            string request;
            string response = null;

            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;

            switch (pProtocol)
            {
                case "whois": //whois
                    if (pLocation == null)
                    {
                        sw.Write(pUsername + "\r\n");
                        sw.Flush();                    
                        Console.WriteLine(pUsername + " is " + sr.ReadToEnd());
                    }//Lookup
                    else
                    {
                        request = pUsername + " " + pLocation + "\r\n";
                        sw.Write(request);
                        sw.Flush();
                        string serverReply = sr.ReadToEnd();
                        if (serverReply == "OK\r\n")
                        {
                            Console.WriteLine(pUsername + " location changed to be " + pLocation);
                        }
                        else
                        {
                            Console.WriteLine("Unexpacted server reply: " + serverReply);
                        }
                    }//Update 
                    break;
                case "-h9": //HTTP 0.9
                    if (pLocation == null)
                    {
                        request = "GET /" + pUsername;
                        sw.WriteLine(request);
                        sw.Flush();
                        client.ReceiveTimeout = 1000;
                        for (int i = 0; i < 4; i++)
                        {
                            response = sr.ReadLine();
                        }
                        Console.WriteLine(pUsername + " is " + response);
                    }//lookup
                    else
                    {
                        request = "PUT /" + pUsername + "\r\n\r\n" + pLocation + "\r\n";
                        sw.Write(request);
                        sw.Flush();
                        Console.WriteLine(pUsername + " location changed to be " + pLocation);
                    }//Update
                    break;
                case "-h0": //HTTP 1.0
                    if (pLocation == null)
                    {
                        request = "GET /?" + pUsername + " HTTP/1.0" + "\r\n";
                        sw.Write(request);
                        sw.Flush();
                        for (int i = 0; i < 4; i++)
                        {
                            response = sr.ReadLine();
                        }
                        Console.WriteLine(pUsername + " is " + response);
                    }//Lookup
                    else
                    {
                        request = "POST /" + pUsername + " HTTP/1.0\r\nContent-Length: " + pLocation.Length + "\r\n\r\n" + pLocation;
                        sw.Write(request);
                        sw.Flush();
                        Console.WriteLine(pUsername + " location changed to be " + sr.ReadToEnd());
                    }//Update
                    break;
                case "-h1": //HTTP 1.1
                    if (pLocation == null)
                    {
                        request = "GET /?name=" + pUsername + " HTTP/1.1\r\nHost: " + pServer + "\r\n";
                        sw.Write(request);
                        sw.Flush();
                        for (int i = 0; i < 4; i++)
                        {
                            response = sr.ReadLine();
                        }
                        Console.WriteLine(pUsername + " is " + response);
                    }//Lookup
                    else
                    {
                        string contentLength = "name=" + pUsername + "&location=" + pLocation;
                        request = "POST / HTTP/1.1\r\nHost: " + pServer + "\r\nContent-Length: " + contentLength.Length + "\r\n\r\nname=" + pUsername + "£location=" + pLocation;
                        sw.Write(request);
                        sw.Flush();
                        Console.WriteLine(pUsername + " location changed to be " + sr.ReadToEnd());
                    }//Update
                    break;
            }
        }
    }
}
