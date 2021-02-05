using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096;
    public Player player;
    public int id;
    public TCP tcp;
    public UDP udp;

    public Client(int _clientId)
    {
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    // TCP setup for the client
    public class TCP
    {
        // Will store the instance we get in the server's TCP connect callback
        public TcpClient socket;

        private readonly int id;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public TCP(int _id)
        {
            id = _id;
        }

        // Will store the connection of our new client to the server through TCP
        public void Connect(TcpClient _socket)
        {
            // Prepare the socket for how large data received and sent should be
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            // Grab our stream of data so we can begin reading
            stream = socket.GetStream();

            // Initialize a packet that we can store our data in
            receivedData = new Packet();

            // Set a limit of how much data we can receive
            receiveBuffer = new byte[dataBufferSize];

            // Begin reading from our stream of data
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            // After we've received the client's connection data, send them a welcome packet
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        // Sends TCP data to the client
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        // Prepares our received data to be read
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                // The size of the data received represented as an int
                int _byteLength = stream.EndRead(_result);

                // If there was no data received then disconnect
                if (_byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                // A new array for storing data from the client; Size is based on what we received as an int
                byte[] _data = new byte[_byteLength];

                // Copy the data we received to the _data byte array
                Array.Copy(receiveBuffer, _data, _byteLength);

                // HandleData will let use know when to reset the packet instance to reuse it for more data
                receivedData.Reset(HandleData(_data));

                // Continue reading any data left in the stream
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error receiving TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        // Determine whether or not we have handled all data
        private bool HandleData(byte[] _data)
        {
            // Initialize a variable that will hold the length of our packet
            int _packetLength = 0;

            // Stores the data received in a packet as bytes
            receivedData.SetBytes(_data);

            // Check if receivedData (our packet) has 4 or more unread bytes, which indicates the start of our packet
            // An int consists of 4 bytes; This data is always at the beginning of a packet and represents its length
            if (receivedData.UnreadLength() >= 4)
            {
                // Since the beginning of the data is greater than or equal to 4 bytes, read its int for our packet's length
                _packetLength = receivedData.ReadInt();

                // If there is no more data, then HandleData returns true which allows the packet to be reset and reused
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            // If we still have data to read, and we have enough room to read that data, 
            //  then continue receiving the next complete packet
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // Store the bytes of the packet into a new byte array
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);

                // Since our code won't be run on the same thread, execute it on the main thread
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();

                        // Pass our handler a packet
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                // Reset the packet length variable and determine if we will begin receiving the next complete packet
                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();

                    // If there is no more data, then HandleData returns true which allows the packet to be reset and reused
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        // Close out our connection with the client through TCP
        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    // UDP setup for the client
    public class UDP
    {
        // Will store the instance we get in the server's UDP connect callback
        public IPEndPoint endPoint;

        public int id;

        public UDP(int _id)
        {
            id = _id;
        }

        // Will store the connection of our new client to the server through UDP
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        // Calls a method to send UDP data to the client
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        // Handle the data packets received from the client
        public void HandleData(Packet _packetData)
        {
            // Read out the current packet's length
            int _packetLength = _packetData.ReadInt();

            // Read out the specified amount of bytes from the packet's length into the data variable
            byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

            // Since our code won't be run on the same thread, execute it on the main thread
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_packetBytes))
                {
                    int _packetId = _packet.ReadInt();

                    // Pass our handler a packet
                    Server.packetHandlers[_packetId](id, _packet);
                }
            });
        }

        // Close out our connection with the client through UDP
        public void Disconnect()
        {
            endPoint = null;
        }
    }

    // Send our connected player into every client's game
    public void SendIntoGame(string _playerName, int _playerColor)
    {
        // Use this loop to check if there is already a player with the specified color
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.player.color == _playerColor)
                {
                    return;
                }
            }
        }

        player = NetworkManager.instance.InstantiatePlayer(id);
        player.Initialize(id, _playerName, _playerColor);
        NetworkManager.instance.playerCount++;

        // Use this loop to send information on our new player to all other connected players (including the new player)
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        // Use this loop to go through our server's dictionary of clients
        // We'll use this dictionary to send the information of all other connected players to our new player
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }
        }
    }

    // Disconnect the client's TCP and UDP instance
    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        // Since our code won't be run on the same thread, execute it on the main thread
        ThreadManager.ExecuteOnMainThread(() =>
        {
            if (player != null)
            {
                // Check if the player is am imposter before disconnecting them
                if (player.isImposter)
                {
                    // If the player is an imposter and they haven't died, subtract from the imposter count
                    if (!player.isDead)
                    {
                        NetworkManager.instance.imposterCount--;
                    }
                }
                else
                {
                    // If the player is a crewmate and they haven't died, subtract from the crewmate count
                    if (!player.isDead)
                    {
                        NetworkManager.instance.crewmateCount--;
                    }
                }

                UnityEngine.Object.Destroy(player.gameObject);
                player = null;
                NetworkManager.instance.playerCount--;
            }
        });

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.DestroyPlayer(id);
    }
}
