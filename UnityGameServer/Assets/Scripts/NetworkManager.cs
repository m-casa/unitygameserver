using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public GameObject playerPrefab;
    private float timer;
    
    // Make sure there is only once instance of this manager
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            timer = 0;
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

        Server.Start(10, 26950);
    }

    // FixedUpdate will be called at the same rate as the tick rate
    public void FixedUpdate()
    {
        // Record at what point in time the last frame finished rendering
        timer += Time.deltaTime;

        // Catch up with the game time.
        // Advance the physics simulation in portions of Time.fixedDeltaTime
        // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
        while (timer >= Time.fixedDeltaTime)
        {
            timer -= Time.fixedDeltaTime;

            // Simulate movement for every character on the server at once
            Physics.Simulate(Time.fixedDeltaTime);
        }
    }

    // Unity editor does not properly close connections when leaving play mode until you enter play mode again
    // So close the connection manually or else the port will be locked
    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    // Return a reference to the player
    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, new Vector3(-8f, 0.3f, -40f), Quaternion.identity).GetComponent<Player>();
    }
}
