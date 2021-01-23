using UnityEngine;

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
        Vector3 _moveDirection = Vector3.zero;
        if (inputs[0] != 0)
        {
            _moveDirection.x = inputs[0];
        }
        if (inputs[1] != 0)
        {
            _moveDirection.y = inputs[1];
        }
        if (inputs[2] != 0)
        {
            _moveDirection.z = inputs[2];
        }

        Move(_moveDirection);
    }

    public void Update()
    {

    }

    // Sends this player's inputs to all clients
    private void Move(Vector3 _moveDirection)
    {
        moveDirection = _moveDirection;

        ServerSend.PlayerInput(this);
        ServerSend.PlayerRotation(this);
    }

    // Stores this player's inputs to the server
    public void SetInput(float[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    // Stores this player's position in the server
    public void SetPosition(float[] _position, Quaternion _rotation)
    {
        transform.position = new Vector3(_position[0], _position[1], _position[2]);
        transform.rotation = _rotation;
    }
}
