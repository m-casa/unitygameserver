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

        Debug.Log($"Received a request from the host (client {_fromClient}) to \"{_msg}\"");

        // Only start a round if there is no active round
        if (!NetworkManager.instance.activeRound)
        {
            NetworkManager.instance.ChooseImposters();
        }
    }

    // Read the packet letting us know to start the meeting
    public static void MeetingRequest(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received a request from client {_fromClient} to \"{_msg}\"");

        // Start a meeting
        NetworkManager.instance.StartMeeting();
    }

    // Read a packet specifying which player was just killed
    public static void KillRequest(int _fromClient, Packet _packet)
    {
        int fromClient = _fromClient;
        int targetId = _packet.ReadInt();

        ServerSend.KillPlayer(fromClient, targetId);

        // Keep track of which crewmate was killed, also set their voting status to true
        //  since they won't be participating in the meetings
        Server.clients[targetId].player.isDead = true;
        Server.clients[targetId].player.voted = true;
        NetworkManager.instance.crewmateCount--;
    }

    // Read the packet letting us know to start the round
    public static void ReportRequest(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received a request from client {_fromClient} to \"{_msg}\"");

        // Report the dead body to all clients
        ServerSend.ReportBody(_fromClient);

        // Start a meeting
        NetworkManager.instance.StartMeeting();
    }

    // Read the packet letting us know which player was voted for
    public static void PlayerVote(int _fromClient, Packet _packet)
    {
        int playerId = _packet.ReadInt();

        Server.clients[_fromClient].player.voted = true;
        ServerSend.PlayerVote(_fromClient, playerId);

        // Check if all players voted, if so end the meeting
        foreach (Client _client in Server.clients.Values)
        {
            // If this player is not dead, reset their voting status
            if (_client.player != null && !_client.player.voted)
            {
                return;
            }
        }

        // If we didn't return out of the PlayerVote method, then that must mean everyone voted
        NetworkManager.instance.meetingTimer = 0;
    }

    // Read the packet letting us know that a task was completed
    public static void CompletedTask(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Client {_fromClient} says they \"{_msg}\"");

        Server.clients[_fromClient].player.completedTasks++;

        NetworkManager.instance.UpdateCompletedTasks(1);
    }

    // Read the packet letting us know there was a request to sabotage lights
    public static void SabotageElectrical(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Client {_fromClient} says they want to \"{_msg}\"");

        NetworkManager.instance.AccessLights();
    }

    // Read the packet letting us know there was a request to fix the lights
    public static void FixElectrical(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Client {_fromClient} says they want to \"{_msg}\"");

        NetworkManager.instance.AccessLights();
    }

    // Read a packet specifying which player was jejected
    public static void ConfirmEject(int _fromClient, Packet _packet)
    {
        int ejectedId = _packet.ReadInt();

        Debug.Log($"Client {_fromClient} wants to confirm eject.");

        NetworkManager.instance.CheckEjectedPlayer(ejectedId);
    }
}
