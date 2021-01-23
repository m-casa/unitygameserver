using UnityEngine;
using ECM.Controllers;

public class Player : MonoBehaviour
{
    public int id;
    public string username;
    public Vector3 moveDirection;

    private float[] inputs;

    // Initialize a new player
    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;

        inputs = new float[3];
    }

    // This is what the server will update every tick
    public void FixedUpdate()
    {
        moveDirection = Vector3.zero;
        if (inputs[0] != 0)
        {
            moveDirection.x = inputs[0];
        }
        if (inputs[1] != 0)
        {
            moveDirection.y = inputs[1];
        }
        if (inputs[2] != 0)
        {
            moveDirection.z = inputs[2];
        }

        SendMovement(moveDirection);
    }

    public void Update()
    {

    }

    // Stores this player's inputs to the server
    public void SetInput(float[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    // Sends this player's inputs to all clients
    private void SendMovement(Vector3 _moveDirection)
    {
        // Store a copy of the client's input for local use
        GetComponent<ServerFirstPersonController>().moveDirection = _moveDirection;

        // Send a copy of the client's input to the other clients
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
        ServerSend.PlayerInput(this);
    }
}
