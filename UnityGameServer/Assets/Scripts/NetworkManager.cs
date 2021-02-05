using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public GameObject playerPrefab;
    public GameObject[] lobbySpawnPoints, shipSpawnPoints;
    public int playerCount, crewmateCount, imposterCount;
    private float timer;
    private bool activeRound;
    
    // Make sure there is only once instance of this manager
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            playerCount = 0;
            crewmateCount = 0;
            imposterCount = 0;
            timer = 0;
            activeRound = false;
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

    // Update will be called at the same rate as the tick rate
    public void Update()
    {
        // If there is an active round, decide when crewmates or imposters win
        if (activeRound)
        {
            if (crewmateCount == imposterCount)
            {
                // End the round and notify players that the imposters have won
                EndRound("Imposters");
            }
            else if (imposterCount == 0)
            {
                // End the round and notify players that the crewmates have won
                EndRound("Crewmates");
            }
        }
    }

    // Unity editor does not properly close connections when leaving play mode until you enter play mode again
    // So close the connection manually or else the port will be locked
    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    // Return a reference to the player
    public Player InstantiatePlayer(int _id)
    {
        return Instantiate(playerPrefab, lobbySpawnPoints[_id - 1].transform.position, Quaternion.identity).GetComponent<Player>();
    }

    // Choose which players will be the imposters
    public void ChooseImposters()
    {
        int rng = Random.Range(1, playerCount + 1);
        int rng2 = rng;

        if (playerCount <= 6)
        {
            Server.clients[rng].player.isImposter = true;

            crewmateCount = playerCount - 1;
            imposterCount = 1;
        }
        else
        {
            while (rng2 == rng)
            {
                rng2 = Random.Range(1, playerCount + 1);
            }

            Server.clients[rng].player.isImposter = true;
            Server.clients[rng2].player.isImposter = true;

            crewmateCount = playerCount - 2;
            imposterCount = 2;
        }

        StartRound();
    }

    // Spawn the players into the ship
    private void StartRound()
    {
        // Send the clients their roles and spawn them in the ship
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.PlayerRole(_client.id, _client.player.isImposter);
                _client.player.transform.position = shipSpawnPoints[_client.id - 1].transform.position;
            }
        }

       activeRound = true;
    }

    // Spawn the players into the lobby
    private void EndRound(string _winningTeam)
    {
        // Send the clients the winning team, reset everyone's roles and spawn them in the lobby
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.Winners(_client.id, _winningTeam);
                _client.player.isImposter = false;
                _client.player.isDead = false;
                _client.player.transform.position = lobbySpawnPoints[_client.id - 1].transform.position;
            }
        }

        activeRound = false;
    }
}
