using UnityEngine;

public class ServerHandle
{
    // Read the packet letting us know the welcome was received
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        // Read in the same order as what was sent
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    // Read the packet letting us know there was player movement
    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        Vector3 _moveDirection = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();
        int _tickNumber = _packet.ReadInt();

        Server.clients[_fromClient].player.SendInput(_moveDirection);
        Server.clients[_fromClient].player.SetMovement(_moveDirection, _rotation, _tickNumber);
    }
}
