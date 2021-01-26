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

    // Store a copy of the client's state on the server
    public void StoreState(Vector3 _moveDirection, Quaternion _rotation, int _tickNumber)
    {
        // Update the server's character with the client's state before movement calculations
        GetComponent<ServerFirstPersonController>().moveDirection = _moveDirection;
        transform.rotation = _rotation;
        GetComponent<ServerFirstPersonController>().tickNumber = _tickNumber;
    }
}
