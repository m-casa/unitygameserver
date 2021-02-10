using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public GameObject playerPrefab;
    public GameObject[] lobbySpawnPoints, shipSpawnPoints;
    public int playerCount, crewmateCount, imposterCount;
    private float simulationTimer, meetingLength, meetingTimer;
    private bool activeRound, activeMeeting, canConfirmEject;
    
    // Make sure there is only once instance of this manager
    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            playerCount = 0;
            crewmateCount = 0;
            imposterCount = 0;
            simulationTimer = 0;
            meetingLength = 35;
            meetingTimer = 0;
            activeRound = false;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    // Initialize the tick rate
    public void Start()
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
        simulationTimer += Time.deltaTime;

        // Catch up with the game time.
        // Advance the physics simulation in portions of Time.fixedDeltaTime
        // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
        while (simulationTimer >= Time.fixedDeltaTime)
        {
            simulationTimer -= Time.fixedDeltaTime;

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

        // If there is an active meeting, decide when to end it
        if (activeMeeting)
        {
            // Gives the player an update on when the meeting will end
            if (meetingTimer + 15 <= 0)
            {
                activeMeeting = false;

                ServerSend.ResumeRound("Resume the current round!");
            }
            else
            {
                meetingTimer -= 1 * Time.deltaTime;

                ServerSend.RemainingTime(meetingTimer);
            }
        }
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

        try
        {
            if (playerCount <= 7)
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
        catch(System.Exception _ex)
        {
            Debug.Log($"Error picking an imposter: {_ex}");
        }
        
    }

    // Spawn all the players back in the cafeteria
    public void StartMeeting()
    {
        // Spawn each player back in the cafeteria and begin the meeting
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                _client.player.transform.position = shipSpawnPoints[_client.id - 1].transform.position;
            }
        }

        ServerSend.Meeting("The meeting has begun!");

        meetingTimer = meetingLength - 15;
        activeMeeting = true;
        canConfirmEject = true;
    }

    // Confirm if the ejected player was an imposter or crewmate
    public void CheckEjectedPlayer(int _ejectedId)
    {
        // Check if the server can confirm an eject or if someone is already being confirmed
        if (canConfirmEject)
        {
            canConfirmEject = false;

            // Make sure the player didn't rage quit and their body is still in the game
            if (Server.clients[_ejectedId].player != null)
            {
                // Keep track of which crewmate was killed
                Server.clients[_ejectedId].player.isDead = true;

                if (Server.clients[_ejectedId].player.isImposter)
                {
                    imposterCount--;
                }
                else
                {
                    crewmateCount--;
                }
            }
        }
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

    // Unity editor does not properly close connections when leaving play mode until you enter play mode again
    // So close the connection manually or else the port will be locked
    private void OnApplicationQuit()
    {
        Server.Stop();
    }
}
