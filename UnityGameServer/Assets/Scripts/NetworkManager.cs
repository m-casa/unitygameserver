using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public GameObject playerPrefab;
    
    // Make sure there is only once instance of this manager
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    // Initialize the tick rate
    private void Start()
    {
        // Don't let the server pump out lots of frames for no reason
        // This also sets our tick rate
        Time.fixedDeltaTime = Constants.SEC_PER_TICK;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Constants.TICKS_PER_SEC;

        // Will prevent the server from running in the editor which locks the port
        #if UNITY_EDITOR
        Debug.Log("Build the project to start the server!");
        #else
        Server.Start(10, 26950);
        #endif
    }

    // Return a reference to the player
    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();
    }
}
