using UnityEngine;

public class ServerHandle
{
    // Read the packet letting us know the welcome was received
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        // Read in the same order as what was sent
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        else
        {
            Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        }
    }

    // Read the packet to spawn the player in everyone's instance
    public static void SpawnRequest(int _fromClient, Packet _packet)
    {
        string _username = _packet.ReadString();
        int _color = _packet.ReadInt();

        Server.clients[_fromClient].SendIntoGame(_username, _color);
    }

    // Read the packet letting us know the client's state before movement calculations
    public static void PlayerState(int _fromClient, Packet _packet)
    {
        Vector3 _moveDirection = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();
        int _tickNumber = _packet.ReadInt();

        if (Server.clients[_fromClient].player != null)
        {
            // Send a copy of the client's movement inputs to the other clients
            ServerSend.PlayerInput(Server.clients[_fromClient].player, _moveDirection);
            ServerSend.PlayerRotation(Server.clients[_fromClient].player, _rotation);

            // Store the client's state on the server
            Server.clients[_fromClient].player.StoreState(_moveDirection, _rotation, _tickNumber);
        }
    }

    // Read the packet letting us know to start the round
    public static void RoundRequest(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received a request from the host to \"{_msg}\"");

        NetworkManager.instance.ChooseImposters();
    }
}
