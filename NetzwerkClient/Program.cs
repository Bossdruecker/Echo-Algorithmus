using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetzwerkClientUDP
{
    class Program
    {
        //22 Kanten 12 Knoten
        public static int[][] nodes = new int[][]
        {
            //new int[] { 2,3,7 },
            //new int[] { 1,3,4,6,10 },
            //new int[] { 1,2 },
            //new int[] { 2,6,8,9,11,12 },
            //new int[] { 9,11 },
            //new int[] { 2,4,9,10 },
            //new int[] { 1,10,12 },
            //new int[] { 4,9,12 },
            //new int[] { 4,5,6,8,11 },
            //new int[] { 2,6,7,12 },
            //new int[] { 4,5,9 },
            //new int[] { 4,7,8,10 }

            new int[] { 2,6,7 },
            new int[] { 1,5,6,8,10 },
            new int[] { 6,10 },
            new int[] { 7,9 },
            new int[] { 2,6,8,10 },
            new int[] { 1,2,3,5 },
            new int[] { 1,4,8,9 },
            new int[] { 2,5,7,9,10 },
            new int[] { 4,7,8 },
            new int[] { 2,3,5,8 }
        };

        static void Main(string[] args)
        {

            List<Verbindung> network = new List<Verbindung>();
            Verbindung myCon = new Verbindung();
            if (args.Length == 0)
            {
                UdpClient udpServer = new UdpClient(5000);  //Port ist 5000 für Main Prozess der andere Prozesse startet
                myCon = DefineNode(GetLocalIP(), 5000, 0);

                for (int i = 1; i < 11; i++)
                {
                    int port = 5000 + i;
                    Process.Start("NetzwerkClient.exe", port.ToString() + " " + i.ToString());
                }

                bool exitintern = true;
                bool exit = false;
                bool check = true;
                while (exitintern)
                {
                    if(check == true)
                    {
                        Byte[] data = ReceiveData(udpServer, 5000);
                        string returnData = Encoding.ASCII.GetString(data);
                        Console.WriteLine(returnData);
                        Verbindung refNode = JsonConvert.DeserializeObject<Verbindung>(returnData);
                        network.Add(refNode);
                        if (network.Count == 10)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                Verbindung temp = network.Find(r => r.NodeNr == (i + 1));
                                SendNeighbor(udpServer, temp, network);
                            }
                            Console.WriteLine("Das Netzwerk wurde erstellt und ist jetzt online\n");
                            Console.WriteLine("Ich bin jetzt im LoggerModus:");
                            check = false;
                        }
                    }
                    else
                    {
                        Byte[] data = ReceiveData(udpServer, 5000);
                        string returnData = Encoding.ASCII.GetString(data);
                        Message msg = JsonConvert.DeserializeObject<Message>(returnData);
                        //INFO oder ECHO Nachrichten formartiert geben
                        if (msg.command == Message.MsgCommand.INFO)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Start:\tIP: " + msg.my_verbindung.Addr.ToString() +
                                "\tPort: " + msg.my_verbindung.Port.ToString() +
                                "\tZiel:\tIP: " + msg.to_verbindung.Addr.ToString() +
                                "\tPort: " + msg.to_verbindung.Port.ToString() +
                                "\tNachrichtentyp: " + msg.command.ToString()
                            );
                        }
                        if(msg.command == Message.MsgCommand.ECHO)
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Start:\tIP: " + msg.my_verbindung.Addr.ToString() +
                                "\tPort: " + msg.my_verbindung.Port.ToString() +
                                "\tZiel:\tIP: " + msg.to_verbindung.Addr.ToString() +
                                "\tPort: " + msg.to_verbindung.Port.ToString() +
                                "\tNachrichtentyp: " + msg.command.ToString() +
                                "\tAktuelle Zwischensumme: " + msg.sum.ToString()
                            );
                        }
                        //Result formatiert ausgeben
                        if (msg.command == Message.MsgCommand.RESULT)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Ergebnis des EchoAlgorithmus: " + msg.sum.ToString());
                            exit = true;
                        }

                        
                        while (exit)
                        {
                            Console.WriteLine("Wollen sie das Netzwerkzurücksetzen oder Beenden?(r/b)");
                            String input = Console.ReadLine();
                            if(input == "r" || input == "b")
                            {
                                if(input == "r")
                                {
                                    ResetNetzwerk(udpServer, network);
                                    Console.WriteLine("Netzwerk wurde Resettet");
                                }
                                else
                                {
                                    ExitNetzwerk(udpServer, network);
                                    exitintern = false;
                                }
                                exit = false;
                            }
                        }
                        
                    }
                    

                    
                }
            }
            else
            {
                UdpClient udpServer = new UdpClient(int.Parse(args[0]));
                myCon = DefineNode(GetLocalIP(), int.Parse(args[0]), int.Parse(args[1]));


                string output = JsonConvert.SerializeObject(myCon);
                Byte[] sendByte = Encoding.ASCII.GetBytes(output);

                IPEndPoint ipEndPoint = new IPEndPoint(GetLocalIP(), 5000);
                SendData(udpServer, ipEndPoint, sendByte);
                Console.WriteLine(myCon.NodeNr.ToString() + " Knoten ist online!");


                //receive Modus um nodes zu empfangen
                while (network.Count != nodes[int.Parse(args[1]) - 1].Length)
                {
                    Byte[] data = ReceiveData(udpServer, int.Parse(args[0]));
                    string returnData = Encoding.ASCII.GetString(data);
                    Console.WriteLine(returnData);
                    Verbindung refNode = JsonConvert.DeserializeObject<Verbindung>(returnData);
                    network.Add(refNode);
                }

                Console.WriteLine(myCon.NodeNr.ToString() + " hat alle Kanten empfange und ist bereit\n");                         // <= ab hier sind die knoten bereit für den verteilten Algorithmus

                Status st = new Status(network.Count, int.Parse(args[1]));

                //Thread notwending um Benutzer eingaben abzufangen alternativ wäre ein Eventhandler
                Thread mySendThread = new Thread(() => SendThread(udpServer, myCon, network, ref st));
                mySendThread.IsBackground = true;
                mySendThread.Start();

                bool exit = true;

                //Echo Algorithmus 
                while (exit)
                {
                    Random waitTime = new Random();
                    int seconds = waitTime.Next(1, 1000);
                    System.Threading.Thread.Sleep(seconds);

                    Byte[] data = ReceiveData(udpServer, myCon.Port);
                    string returnData = Encoding.ASCII.GetString(data);
                    Message msg = JsonConvert.DeserializeObject<Message>(returnData);
                    Console.WriteLine(msg.command.ToString() + " von " + msg.my_verbindung.Port.ToString() + " count " + st.Informed_Nachbarn.ToString());
                    //InformLogger(msg.my_verbindung, msg.to_verbindung, msg, udpServer);

                    st.Informed_Nachbarn += 1;
                    if(msg.command == Message.MsgCommand.INFO)
                    {
                        if(st.Informed == false)
                        {
                            st.Informed = true;
                            st.Upward_Node = msg.my_verbindung;

                            foreach (Verbindung verb in network)
                            {
                                if (verb.Port != st.Upward_Node.Port)
                                {
                                    msg = new Message(myCon, verb, Message.MsgCommand.INFO, 0);
                                    output = JsonConvert.SerializeObject(msg);
                                    sendByte = Encoding.ASCII.GetBytes(output);
                                    SendData(udpServer, new IPEndPoint(IPAddress.Parse(verb.Addr), verb.Port), sendByte);
                                    InformLogger(msg.my_verbindung, msg.to_verbindung, msg, udpServer);
                                }
                            }
                        }
                    }
                    if(msg.command == Message.MsgCommand.ECHO)
                    {
                        st.GesamtSpeicher += msg.sum;
                    }
                    if(st.Informed_Nachbarn == st.AnzahlNachbarn)
                    {
                        if(st.Initiator == false)
                        {
                            msg = new Message(myCon, st.Upward_Node, Message.MsgCommand.ECHO, st.GesamtSpeicher);
                            output = JsonConvert.SerializeObject(msg);
                            sendByte = Encoding.ASCII.GetBytes(output);
                            SendData(udpServer, new IPEndPoint(IPAddress.Parse(st.Upward_Node.Addr), st.Upward_Node.Port), sendByte);
                            InformLogger(msg.my_verbindung, msg.to_verbindung, msg, udpServer);
                        }
                        else
                        {
                            msg = new Message(myCon, st.Upward_Node, Message.MsgCommand.RESULT, st.GesamtSpeicher);
                            InformLogger(msg.my_verbindung, msg.to_verbindung, msg, udpServer);
                        }
                    }
                    if(msg.command == Message.MsgCommand.RESET)
                    {
                        st.resetStatus();
                        Console.WriteLine("Node wurde Resettet");
                    }
                    if(msg.command == Message.MsgCommand.EXIT)
                    {
                        mySendThread.Abort();
                        exit = false;
                    }

                }
            }
        }

        //Thread der Konsolen eingaben abfängt
        private static void SendThread(UdpClient server, Verbindung mycon, List<Verbindung> netw, ref Status st)
        {
            bool exit = true;
            while (exit)
            {
                string input = Console.ReadLine();
                if (input == "!Start")
                {
                    Console.WriteLine("Algo wird gestartet");
                    st.Informed = true;
                    st.Initiator = true;
                    StartAlgo(server, mycon, netw, ref st);
                }
                else if (input == "!Exit")
                {
                    exit = false;
                    Console.WriteLine("Thread Send wird beendet");
                }
                else
                {
                    Console.WriteLine("keine Korrekte Eingabe");
                }
            }
        }

        //Funktion bestimmt die locale IP
        private static IPAddress GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            return IPAddress.Parse("127.0.0.1");
        }

        //Funktion erstellt einen Knoten
        private static Verbindung DefineNode(IPAddress addr, int port, int num)
        {
            Verbindung verb = new Verbindung
            {
                Addr = addr.ToString(),
                Port = port,
                NodeNr = num
            };
            return verb;
        }

        //Funktion ist dazu da um allen Knoten ihre Nachbarn mitzuteilen
        private static void SendNeighbor(UdpClient udpServer, Verbindung verb, List<Verbindung> network)
        {
            for (int i = nodes[verb.NodeNr - 1].Length; i > 0; i--)
            {
                Verbindung temp = network.Find(r => r.NodeNr == nodes[verb.NodeNr - 1][i - 1]);
                string output = JsonConvert.SerializeObject(temp);
                Byte[] sendByte = Encoding.ASCII.GetBytes(output);
                SendData(udpServer, new IPEndPoint(IPAddress.Parse(verb.Addr), verb.Port), sendByte);
            }
        }

        //Funktion Daten empfangen und als Byte Array zurückgeben
        private static Byte[] ReceiveData(UdpClient udpServer, int port)
        {
            Byte[] data = null;
            var groupEP = new IPEndPoint(IPAddress.Any, port);
            data = udpServer.Receive(ref groupEP);
            return data;
        }

        //Byte Array schicken
        private static void SendData(UdpClient udpServer, IPEndPoint ipEndPoint, Byte[] sendByte)
        {
            udpServer.Send(sendByte, sendByte.Length, ipEndPoint);
        }

        //Funktion startet den Algorithmus
        private static void StartAlgo(UdpClient server, Verbindung myCon, List<Verbindung> netw, ref Status myStatus)
        {
            myStatus.resetStatus();
            myStatus.Initiator = true;
            myStatus.Informed = true;

            foreach (Verbindung element in netw)
            {
                Message msg = new Message(myCon, element, Message.MsgCommand.INFO, 0);
                string output = JsonConvert.SerializeObject(msg);
                Byte[] sendByte = Encoding.ASCII.GetBytes(output);
                SendData(server, new IPEndPoint(IPAddress.Parse(element.Addr), element.Port), sendByte);
                Console.WriteLine("Info an: " + element.Port.ToString());
                InformLogger(myCon, element, msg, server);
                //Console.ReadKey();
            }
        }

        //Funktion die den Logger Informiert
        private static void InformLogger(Verbindung myCon, Verbindung otherCon, Message msg, UdpClient udpServer)
        {
            String output = JsonConvert.SerializeObject(msg);
            Byte[] sendByte = Encoding.ASCII.GetBytes(output);
            SendData(udpServer, new IPEndPoint(GetLocalIP(), 5000), sendByte);
        }

        private static void ResetNetzwerk(UdpClient udpServer, List<Verbindung> network)
        {
            foreach(Verbindung element in network)
            {
                Message msg = new Message(element, null, Message.MsgCommand.RESET, 0);
                String output = JsonConvert.SerializeObject(msg);
                Byte[] sendByte = Encoding.ASCII.GetBytes(output);
                SendData(udpServer, new IPEndPoint(IPAddress.Parse(element.Addr), element.Port), sendByte);
            }
        }

        private static void ExitNetzwerk(UdpClient udpServer, List<Verbindung> network)
        {
            foreach (Verbindung element in network)
            {
                Message msg = new Message(element, null, Message.MsgCommand.EXIT, 0);
                String output = JsonConvert.SerializeObject(msg);
                Byte[] sendByte = Encoding.ASCII.GetBytes(output);
                SendData(udpServer, new IPEndPoint(IPAddress.Parse(element.Addr), element.Port), sendByte);
            }
        }
    }
}

