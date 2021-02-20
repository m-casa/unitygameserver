
using UnityEngine;

public class ServerSend
{
    // Send data to a specific client using TCP
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    // Send data to all clients using TCP
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.maxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }

    // Send data to all clients except a specific one using TCP
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.maxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    // Send data to a specific client using UDP
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    // Send data to all clients using UDP
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.maxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }

    // Send data to all clients except a specific one using UDP
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.maxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    #region Packets

    // Sends a welcome packet
    public static void Welcome(int _toClient, string _msg)
    {
        // "Using" automatically disposes the packet for us when it's done being used
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    // Sends a packet to the client with player spawn position information
    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.colorId);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            SendTCPData(_toClient, _packet);
        }
    }

    // Sends a packet to the client with player input information
    public static void PlayerInput(Player _player, Vector3 _moveDirection)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerInput))
        {
            _packet.Write(_player.id);
            _packet.Write(_moveDirection);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    // Sends a packet to the client with player rotation information
    public static void PlayerRotation(Player _player, Quaternion _rotation)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_rotation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    // Sends a packet to the client with player position information
    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    // Sends a packet to the client with the server's character state
    public static void PlayerState(int _toClient, Vector3 _position, int _tickNumber)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerState))
        {
            _packet.Write(_toClient);
            _packet.Write(_position);
            _packet.Write(_tickNumber);

            SendUDPDataToAll(_packet);
        }
    }

    // Sends a packet to the client letting them know the player's role
    public static void PlayerRole(int _toClient, bool _isImposter)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRole))
        {
            _packet.Write(_toClient);
            _packet.Write(_isImposter);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server letting them know to attend the meeting
    public static void Meeting(string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.meeting))
        {
            _packet.Write(_msg);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server with the remaining time for the meeting
    public static void RemainingTime(float _meetingTimer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.remainingTime))
        {
            _packet.Write(_meetingTimer);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server letting them know who was voted for
    public static void PlayerVote(int _fromClient, int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerVote))
        {
            _packet.Write(_fromClient);
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server letting them know to resume the round
    public static void ResumeRound(string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.resumeRound))
        {
            _packet.Write(_msg);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server letting them know which player died
    public static void KillPlayer(int _fromClient, int _targetId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.killPlayer))
        {
            _packet.Write(_targetId);

            SendTCPDataToAll(_fromClient, _packet);
        }
    }

    // Sends a packet to everyone on the server letting them know to despawn any dead bodies
    public static void ReportBody(int _reporter)
    {
        using (Packet _packet = new Packet((int)ServerPackets.reportBody))
        {
            _packet.Write(_reporter);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server that updates the task bar
    public static void TaskUpdate(float _updatedValue)
    {
        using (Packet _packet = new Packet((int)ServerPackets.taskUpdate))
        {
            _packet.Write(_updatedValue);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to close a specific door
    public static void CloseDoor(int _doorId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.closeDoor))
        {
            _packet.Write(_doorId);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to open a specific door
    public static void OpenDoor(int _doorId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.openDoor))
        {
            _packet.Write(_doorId);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to turn off the lights
    public static void TurnOffLights()
    {
        using (Packet _packet = new Packet((int)ServerPackets.turnOffLights))
        {
            _packet.Write("Turn the lights off!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to turn on the lights
    public static void TurnOnLights()
    {
        using (Packet _packet = new Packet((int)ServerPackets.turnOnLights))
        {
            _packet.Write("Turn on the lights!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to turn off the oxygen
    public static void TurnOffO2()
    {
        using (Packet _packet = new Packet((int)ServerPackets.turnOffO2))
        {
            _packet.Write("Turn off the oxygen!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to turn on the oxygen
    public static void TurnOnO2()
    {
        using (Packet _packet = new Packet((int)ServerPackets.turnOnO2))
        {
            _packet.Write("Turn on the oxygen!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to meltdown the reactor
    public static void MeltdownReactor()
    {
        using (Packet _packet = new Packet((int)ServerPackets.meltdownReactor))
        {
            _packet.Write("Meltdowen the reactor!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server to restore the reactor
    public static void RestoreReactor()
    {
        using (Packet _packet = new Packet((int)ServerPackets.restoreReactor))
        {
            _packet.Write("Restore the reactor!");

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server with the remaining game time
    public static void RemainingGameTime(float _remainingTime)
    {
        using (Packet _packet = new Packet((int)ServerPackets.remainingGameTime))
        {
            _packet.Write(_remainingTime);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server with remaining sabotage cooldown
    public static void TimeToSabotage(float _timeToSabotage)
    {
        using (Packet _packet = new Packet((int)ServerPackets.timeToSabotage))
        {
            _packet.Write(_timeToSabotage);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to everyone on the server letting them know which team won
    public static void Winners(int _toClient, string _winningTeam)
    {
        using (Packet _packet = new Packet((int)ServerPackets.winners))
        {
            _packet.Write(_toClient);
            _packet.Write(_winningTeam);

            SendTCPDataToAll(_packet);
        }
    }

    // Sends a packet to the client letting them know which player to destroy
    public static void DestroyPlayer(int _id)
    {
        using (Packet _packet = new Packet((int)ServerPackets.destroyPlayer))
        {
            _packet.Write(_id);

            SendTCPDataToAll(_id, _packet);
        }
    }

    #endregion
}
