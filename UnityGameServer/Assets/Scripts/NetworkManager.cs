using UnityEngine;
using ECM.Controllers;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public GameObject playerPrefab;
    public GameObject[] lobbySpawnPoints, shipSpawnPoints, doors;
    public float totalTasks, completedTasks, meetingTimer, 
        sabotageCooldown, currentCooldown, timeToWinGame, remainingGameTime;
    public int playerCount, crewmateCount, imposterCount, O2Count;
    public bool activeRound, activeSabotage, activeCooldown, activeEndGame,
        pad1BeingHeld, pad2BeingHeld;
    private float simulationTimer, meetingLength;
    private bool activeMeeting, canConfirmEject;
    
    // Make sure there is only once instance of this manager
    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            playerCount = 0;
            crewmateCount = 0;
            imposterCount = 0;
            totalTasks = 0;
            sabotageCooldown = 35;
            timeToWinGame = 40;
            activeRound = false;
            activeSabotage = false;
            activeCooldown = false;
            activeEndGame = false;
            simulationTimer = 0;
            meetingLength = 130;
        }
        else if (instance != this)
        {
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
            UpdateRoundStatus();
        }

        // If there is an active meeting, decide when to end it
        if (activeMeeting)
        {
            UpdateMeetingTime();
        }

        // Check if the cooldown needs to go down in order to start sabotages again
        if (activeCooldown)
        {
            UpdateSabotageCooldown();
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
    public void StartMeeting(int _beginType)
    {
        // If there's a sabotage that can end the game, reset it
        if (activeEndGame)
        {
            activeSabotage = false;
            activeEndGame = false;
            ServerSend.TurnOnO2(3);
            ServerSend.RestoreReactor();
        }

        // Spawn each player back in the cafeteria and begin the meeting
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                _client.player.transform.position = shipSpawnPoints[_client.id - 1].transform.position;
                _client.player.GetComponent<ServerFirstPersonController>().moveDirection = Vector3.zero;
            }
        }

        ServerSend.Meeting(_beginType);

        meetingTimer = meetingLength - 10;
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

    // Updated the total completed tasks, and send the update to every client
    public void UpdateCompletedTasks(float _numOfTasks)
    {
        float updatedValue;

        completedTasks += _numOfTasks;

        updatedValue = (totalTasks - (totalTasks - completedTasks)) / totalTasks * 100;

        ServerSend.TaskUpdate(updatedValue);
    }

    // Turn off the lights for everyone if they are currently on
    public void TurnOffLights()
    {
        if (!activeSabotage)
        {
            activeSabotage = true;
            ServerSend.TurnOffLights();
        }
    }

    // Turn on the lights for everyone if they are currently off
    public void TurnOnLights()
    {
        activeSabotage = false;
        currentCooldown = sabotageCooldown;
        activeCooldown = true;
        ServerSend.TurnOnLights();
    }

    // Turn off the oxygen for everyone if it is currently on
    public void TurnOffO2()
    {
        if (!activeSabotage)
        {
            activeSabotage = true;
            O2Count = 0;
            ServerSend.TurnOffO2();
            remainingGameTime = timeToWinGame;
            activeEndGame = true;
        }
    }

    // Turn on the oxygen for everyone if it is currently off
    public void TurnOnO2(int _O2PadId)
    {
        if (O2Count < 1)
        {
            O2Count++;
            ServerSend.TurnOnO2(_O2PadId);
        }
        else
        {
            activeSabotage = false;
            activeEndGame = false;
            currentCooldown = sabotageCooldown;
            activeCooldown = true;
            ServerSend.TurnOnO2(3);
        }
    }

    // Meltdown the reactor for everyone if not sabotaged
    public void MeltdownReactor()
    {
        if (!activeSabotage)
        {
            activeSabotage = true;
            ServerSend.MeltdownReactor();
            remainingGameTime = timeToWinGame;
            activeEndGame = true;
        }
    }

    // Restore the reactor for everyone if it is melting down
    public void RestoreReactor(int _reactorPadId, bool _isBeingHeld)
    {
        if (_reactorPadId == 0)
        {
            pad1BeingHeld = _isBeingHeld;
        }
        else if (_reactorPadId == 1)
        {
            pad2BeingHeld = _isBeingHeld;
        }

        if (pad1BeingHeld && pad2BeingHeld)
        {
            activeSabotage = false;
            activeEndGame = false;
            currentCooldown = sabotageCooldown;
            activeCooldown = true;
            ServerSend.RestoreReactor();
            pad1BeingHeld = false;
            pad2BeingHeld = false;
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

                // Setup the number of tasks needed to win
                if (!_client.player.isImposter)
                {
                    totalTasks += 11;
                }
            }
        }

        completedTasks = 0;
        UpdateCompletedTasks(0);
        activeRound = true;

        currentCooldown = sabotageCooldown;
        activeCooldown = true;
    }

    // Will update the players whether or not a team has won
    private void UpdateRoundStatus()
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
        else if (completedTasks == totalTasks)
        {
            // End the round and notify players that the crewmates have won
            EndRound("Crewmates");
        }
        else if (activeEndGame)
        {
            if (remainingGameTime <= 0)
            {
                activeEndGame = false;

                // End the round and notify players that the imposters have won
                EndRound("Imposters");
            }
            else
            {
                remainingGameTime -= 1 * Time.deltaTime;

                ServerSend.RemainingGameTime(remainingGameTime);
            }
        }
    }

    // Will update the players on the remaining meeting time
    private void UpdateMeetingTime()
    {
        // Gives the player an update on when the meeting will end
        if (meetingTimer + 10 <= 0)
        {
            activeMeeting = false;

            // If there's no active sabotage, reset the cooldown
            if (!activeSabotage)
            {
                currentCooldown = sabotageCooldown;
                activeCooldown = true;
            }

            ServerSend.ResumeRound("Resume the current round!");

            // Loop through every player
            foreach (Client _client in Server.clients.Values)
            {
                // If this player is not dead, reset their voting status
                if (_client.player != null && !_client.player.isDead)
                {
                    _client.player.voted = false;
                }
            }
        }
        else
        {
            meetingTimer -= 1 * Time.deltaTime;

            ServerSend.RemainingTime(meetingTimer);
        }
    }

    // Will update the imposters on when they can sabotage
    private void UpdateSabotageCooldown()
    {
        if (currentCooldown <= 0)
        {
            activeCooldown = false;

            ServerSend.TimeToSabotage(currentCooldown);
        }
        else
        {
            currentCooldown -= 1 * Time.deltaTime;

            ServerSend.TimeToSabotage(currentCooldown);
        }
    }

    // Spawn the players into the lobby
    private void EndRound(string _winningTeam)
    {
        activeRound = false;
        activeSabotage = false;
        activeEndGame = false;
        completedTasks = 0;
        totalTasks = 0;
        ServerSend.TimeToSabotage(0);
        foreach (GameObject door in doors)
        {
            door.SetActive(false);
        }

        // Send the clients the winning team, reset everyone's roles and spawn them in the lobby
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.Winners(_client.id, _winningTeam);
                _client.player.isImposter = false;
                _client.player.isDead = false;
                _client.player.voted = false;
                _client.player.completedTasks = 0;
                _client.player.transform.position = lobbySpawnPoints[_client.id - 1].transform.position;
            }
        }
    }

    // Unity editor does not properly close connections when leaving play mode until you enter play mode again
    // So close the connection manually or else the port will be locked
    private void OnApplicationQuit()
    {
        Server.Stop();
    }
}
