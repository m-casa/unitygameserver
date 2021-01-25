using UnityEngine;
using ECM.Controllers;

public class Player : MonoBehaviour
{
    public int id;
    public string username;

    // Initialize a new player
    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
    }

    // Any FixedUpdate is what the server will update/send every tick
    public void FixedUpdate()
    {

    }

    // Store the client's movement information on the server
    public void SetMovement(Vector3 _moveDirection, Quaternion _rotation, int _tickNumber)
    {
        // Update the server's characters with movement/rotation information
        GetComponent<ServerFirstPersonController>().moveDirection = _moveDirection;
        transform.rotation = _rotation;

        // Simulate movement for the characters by calling Physics.Simulate
        // This will esentially run FixedUpdate manually
        Physics.Simulate(Time.fixedDeltaTime);

        // Update the tick to signify we've reached the end of the simulation
        //  then send the server's position/rotation information of all players
        SendPosition(_tickNumber + 1);
    }

    // Send a copy of the client's movement inputs to the other clients
    public void SendInput(Vector3 _moveDirection)
    {
        ServerSend.PlayerInput(this, _moveDirection);
    }

    // Send a copy of the server's players to the other clients
    public void SendPosition(int _tickNumber)
    {
        ServerSend.PlayerPosition(this, _tickNumber);
        ServerSend.PlayerRotation(this);
    }
}
