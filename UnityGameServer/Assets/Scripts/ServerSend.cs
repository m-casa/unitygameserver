
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
            _packet.Write(_player.color);
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

    // Sends a packet to everyone on the server letting them know which player died
    public static void KillPlayer(int _fromClient, int _targetId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.killPlayer))
        {
            _packet.Write(_targetId);

            SendTCPDataToAll(_fromClient, _packet);
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
