using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int maxPlayers { get; private set; }
    public static int port { get; private set; }

    // A new dictionary to keep track of our clients and their ids
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();

    // A delegate basically says "feel free to assign any method to this delegate if the signature matches"
    // Since our HandleData methods in the Client class have a "using" that matches the "Packet _packet" signature,
    //  we know that is where the packet is being handled, hence this delegate's name
    public delegate void PacketHandler(int _fromClient, Packet _packet);

    // A new dictionary to keep track of our packet handlers and their ids
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    // Will start our server with the specified max players and port number
    public static void Start(int _maxPlayers, int _port)
    {
        maxPlayers = _maxPlayers;
        port = _port;

        Debug.Log("Starting server...");

        InitializeServerData();

        // Will listen in on the specified port with TCP for any IP Address trying to connect
        tcpListener = new TcpListener(IPAddress.Any, port);

        // Start listening
        tcpListener.Start();

        // Accept any client that attempts to connect
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        // Will listen in on the specified port with UDP for any IP Address trying to connect
        udpListener = new UdpClient(port);

        // Accept any client that attempts to connect
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on {port}.");
    }

    // Will handle a client connection through TCP
    private static void TCPConnectCallback(IAsyncResult _result)
    {
        // Store our client's connection attempt
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);

        // Continue listening for client connections
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        Debug.Log($"Incoming connection from {_client.Client.RemoteEndPoint}...");

        // Need to assign our newly connected clients their id
        for (int i = 1; i <= maxPlayers; i++)
        {
            // If the socket our client is trying to use is not being used,
            //  store our client's information through that socket
            if (clients[i].tcp.socket == null)
            {
                // Connect the client to the server using TCP
                // NOTE: _client and _socket are interchangeable names
                clients[i].tcp.Connect(_client);
                return;
            }
        }

        Debug.Log($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    // Will handle a client connection through UDP
    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            // Initialize an IPEndPoint that will store our client's connection attempt
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // This method will not only return any bytes received, 
            //  but will also set our IPEndPoint to the endpoint where the data came from
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);

            // Continue listening for client connections
            udpListener.BeginReceive(UDPReceiveCallback, null);

            // Disconnect the client if the data received is less than 4 bytes
            // NOTE: Might not have to disconnect, as it could be a common occurence that data is less than 4 bytes
            if (_data.Length < 4)
            {
                // TODO: disconnect
                return;
            }

            using (Packet _packet = new Packet(_data))
            {
                int _clientId = _packet.ReadInt();

                // Make sure the client's id is not 0, as this id does not exist and can cause a server crash
                if (_clientId == 0)
                {
                    return;
                }

                // Check if the UDP endpoint is null, which means this is a new connection
                //  and the packet received is the empty one that opens up the client's port
                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                // Check to make sure the endpoint we have stored for the client matches the client id of where the packet came from
                // Stops hackers from trying to impersonate another client
                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    // Pass our handlers any data that needs to be read
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error receiving UDP data: {_ex}");
        }
    }

    // Sends UDP data to the client
    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null)
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
        }
    }

    // Adds unassigned clients to our dictionary equal to the max number of players
    // As actual players join the server, they will be assigned a client id from the dictionary
    // Also initializes our packet handlers
    private static void InitializeServerData()
    {
        for (int i = 1; i <= maxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        // These packets are for receiving
        packetHandlers = new Dictionary<int, PacketHandler>
        {
            { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
            { (int)ClientPackets.playerState, ServerHandle.PlayerState },
        };
        Debug.Log("Initialized packets.");
    }

    // Closes our TCP and UDP connections on the server
    public static void Stop()
    {
        tcpListener.Stop();
        udpListener.Close();
    }
}
