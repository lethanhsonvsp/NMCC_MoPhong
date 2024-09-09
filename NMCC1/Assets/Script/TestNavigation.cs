using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class Waypoint
{
    public Transform destination;
    public float delayDuration = 0f;
}

public class TestNavigation : MonoBehaviour
{
    private NavMeshAgent agent;

    // Rotation variables
    private bool isRotating = false;
    private float rotationStartTime;
    private float rotationDuration = 1f;

    // Stuck check variables
    private float stuckCheckTime;
    private float stuckCheckInterval = 0f;
    private float stopThreshold = 1f;

    // Configuration variables
    public GameObject TKER;
    public string[] specifiedNames;
    private int maxSpecifiedRepeats;

    // Waypoint sets
    public Waypoint[] destinations0, destinations4, destinations5;
    public Transform[] A, BL, BR, CL, CR, D, E;
    public Transform[] intermediateDestinationsA, intermediateDestinationsB, intermediateDestinationsC, intermediateDestinationsD, intermediateDestinationsE;
    public Transform[] intermediate2DestinationsA, intermediate2DestinationsBL, intermediate2DestinationsBR, intermediate2DestinationsCL, intermediate2DestinationsCR, intermediate2DestinationsD, intermediate2DestinationsE;

    // PLER game objects
    public GameObject[] PLERA, PLERBL, PLERBR, PLERCL, PLERCR, PLERD, PLERE;

    // PLER state
    private GameObject PLER;
    private bool plerWaitingForTker = true;
    private bool plerShouldFollowTker = false;
    private float speedPLER = 3.5f;

    // Navigation state
    private int currentDestinationIndex = 0;
    private int currentDestinationSet = 0;
    private bool hasReachedDestination = false;
    private int repeatCount = 0;
    private int stepInRepeatCycle = 0;
    private bool isWaiting = false;
    private bool handleDestination1To3Phase = true;
    private bool secondHandle1To3Phase = false;
    private int indexSP;
    private int handleDestination1To3Counter;
    private bool hasRunFirstHandle1To3 = false;
    private int delayCoroutineCallCount = 0;

    // Intermediate positions
    private Transform SpecifiedPosition, intermediatePosition, intermediate2Position, intermediatePreviousPosition, intermediateNextPosition, intermediateNextPosition1;
    private Transform[] SetArray;

    private Dictionary<string, Transform[]> destinationArrays;
    private Dictionary<string, Transform[]> intermediateDestinations;
    private Dictionary<string, Transform[]> intermediate2Destinations;
    private Dictionary<string, GameObject[]> PLERs;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetNextDestination();
        stuckCheckTime = Time.time;
        maxSpecifiedRepeats = specifiedNames.Length;
    }

    void Update()
    {
        if (!isWaiting) FollowTker();

        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance <= stopThreshold)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                hasReachedDestination = true;
                SetNextDestination();
                return;
            }
        }

        if (isRotating)
        {
            RotateTowardsMovementDirection();

            if (Time.time >= rotationStartTime + rotationDuration)
            {
                isRotating = false;
                agent.isStopped = false;
            }
        }
        else
        {
            if (agent.hasPath && ShouldRotateTowardsMovementDirection())
            {
                StartRotation();
            }

            if (Time.time >= stuckCheckTime + stuckCheckInterval)
            {
                if (agent.velocity.sqrMagnitude < 0.01f)
                {
                    agent.Move(transform.forward * 0.01f);
                }
                stuckCheckTime = Time.time;
            }
        }
    }

    private void FollowTker()
    {
        if (plerShouldFollowTker && PLER != null && TKER != null)
        {
            if (plerWaitingForTker)
            {
                PLER.transform.rotation = Quaternion.identity;
                if (SpecifiedPosition != null && Vector3.Distance(TKER.transform.position, SpecifiedPosition.position) <= stopThreshold)
                {
                    plerWaitingForTker = false;
                }
            }
            else
            {
                Vector3 targetPosition = new Vector3(
                    TKER.transform.position.x,
                    TKER.transform.position.y,
                    TKER.transform.position.z
                );
                PLER.transform.position = Vector3.MoveTowards(
                    PLER.transform.position,
                    targetPosition,
                    speedPLER * Time.deltaTime
                );
                PLER.transform.rotation = Quaternion.RotateTowards(
                    PLER.transform.rotation,
                    TKER.transform.rotation,
                    180 * Time.deltaTime
                );
            }
        }
    }

    void Awake()
    {
        destinationArrays = new Dictionary<string, Transform[]>
        {
            {"A", A}, {"BL", BL}, {"BR", BR}, {"CL", CL}, {"CR", CR}, {"D", D}, {"E", E}
        };

        intermediateDestinations = new Dictionary<string, Transform[]>
        {
            {"A", intermediateDestinationsA}, {"B", intermediateDestinationsB},
            {"C", intermediateDestinationsC}, {"D", intermediateDestinationsD},
            {"E", intermediateDestinationsE}
        };

        intermediate2Destinations = new Dictionary<string, Transform[]>
        {
            {"A", intermediate2DestinationsA}, {"BL", intermediate2DestinationsBL},
            {"BR", intermediate2DestinationsBR}, {"CL", intermediate2DestinationsCL},
            {"CR", intermediate2DestinationsCR}, {"D", intermediate2DestinationsD},
            {"E", intermediate2DestinationsE}
        };

        PLERs = new Dictionary<string, GameObject[]>
        {
            {"A", PLERA}, {"BL", PLERBL}, {"BR", PLERBR}, {"CL", PLERCL},
            {"CR", PLERCR}, {"D", PLERD}, {"E", PLERE}
        };
    }

    private void SetNextDestination()
    {
        if (!hasReachedDestination || isWaiting) return;

        switch (currentDestinationSet)
        {
            case 0:
                HandleDestination0();
                break;
            case 1:
                if (repeatCount < maxSpecifiedRepeats)
                {
                    if (handleDestination1To3Phase)
                    {
                        HandleDestination1To3();
                    }
                    else
                    {
                        HandleDestination4();
                    }
                }
                else
                {
                    currentDestinationSet++;
                    if (currentDestinationSet == 2)
                    {
                        if (secondHandle1To3Phase)
                        {
                            HandleDestination1To3();
                        }
                        else
                        {
                            SetNextDestination();
                        }
                    }
                    else if (currentDestinationSet == 4)
                    {
                        HandleDestination5();
                    }
                    else if (currentDestinationSet > 4)
                    {
                        agent.isStopped = true;
                        Debug.Log("Completed all destinations");
                    }
                    else
                    {
                        SetNextDestination();
                    }
                }
                break;
            case 2:
                if (secondHandle1To3Phase)
                {
                    HandleDestination1To3();
                }
                else
                {
                    SetNextDestination();
                }
                break;
            case 4:
                HandleDestination5();
                break;
            default:
                agent.isStopped = true;
                Debug.Log("Completed all destinations");
                break;
        }
    }

    private void HandleDestination0()
    {
        if (currentDestinationIndex < destinations0.Length)
        {
            agent.destination = destinations0[currentDestinationIndex].destination.position;
            float delay = destinations0[currentDestinationIndex].delayDuration;
            currentDestinationIndex++;
            hasReachedDestination = false;

            if (delay > 0)
            {
                StartCoroutine(DelayCoroutine(delay));
            }
        }
        else
        {
            currentDestinationSet = 1;
            currentDestinationIndex = 0;
            repeatCount = 0;
            stepInRepeatCycle = 0;
            SetNextDestination();
        }
    }

    private void HandleDestination1To3()
    {
        if (indexSP >= specifiedNames.Length)
        {
            currentDestinationSet = 4;
            SetNextDestination();
            return;
        }

        switch (stepInRepeatCycle)
        {
            case 0:
                SelectSpecifiedPosition();
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 1:
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 2:
                UpdateIntermediateNextPosition();
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 3:
                UpdateIntermediateNextPosition1();
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 4:
                agent.destination = SetArray[0].position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 5:
                agent.destination = intermediatePosition.position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 6:
                agent.destination = intermediate2Position.position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 7:
                agent.destination = SpecifiedPosition.position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 8:
                StartCoroutine(DelayCoroutine(1f, true));
                hasReachedDestination = false;
                stepInRepeatCycle++;
                break;
            case 9:
                agent.destination = intermediate2Position.position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 10:
                agent.destination = intermediatePosition.position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 11:
                agent.destination = SetArray[0].position;
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 12:
                UpdateIntermediateNextPosition1();
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 13:
                UpdateIntermediateNextPosition();
                stepInRepeatCycle++;
                hasReachedDestination = false;
                break;
            case 14:
                stepInRepeatCycle = 0;
                hasReachedDestination = false;
                handleDestination1To3Counter++;
                if (handleDestination1To3Counter >= 1)
                {
                    if (!hasRunFirstHandle1To3)
                    {
                        UpdateIntermediateNextPosition();
                        hasRunFirstHandle1To3 = true;
                        handleDestination1To3Phase = false;
                        secondHandle1To3Phase = true;
                    }
                    else
                    {
                        handleDestination1To3Counter = 0;
                        indexSP++;
                        hasRunFirstHandle1To3 = false;
                        if (indexSP < specifiedNames.Length)
                        {
                            UpdateIntermediateNextPosition();
                            handleDestination1To3Phase = true;
                            SelectSpecifiedPosition();
                        }
                        else
                        {
                            UpdateIntermediateNextPosition();
                            handleDestination1To3Phase = false;
                        }
                    }
                }
                break;
            default:
                Debug.LogError("Invalid stepInRepeatCycle value: " + stepInRepeatCycle);
                break;
        }
    }

    private void HandleDestination4()
    {
        if (currentDestinationIndex < destinations4.Length)
        {
            agent.destination = destinations4[currentDestinationIndex].destination.position;
            float delay = destinations4[currentDestinationIndex].delayDuration;
            currentDestinationIndex++;
            hasReachedDestination = false;

            if (delay > 0)
            {
                StartCoroutine(DelayCoroutine(delay));
            }
        }
        else
        {
            currentDestinationIndex = 0;
            repeatCount++;
            handleDestination1To3Phase = true;
            SetNextDestination();
        }
    }

    private void HandleDestination5()
    {
        if (currentDestinationIndex < destinations5.Length)
        {
            agent.destination = destinations5[currentDestinationIndex].destination.position;
            float delay = destinations5[currentDestinationIndex].delayDuration;
            currentDestinationIndex++;
            hasReachedDestination = false;

            if (delay > 0)
            {
                StartCoroutine(DelayCoroutine(delay));
            }
        }
        else
        {
            agent.isStopped = true;
            //Debug.Log("Completed all destinations");
        }
    }

    private void UpdateIntermediateNextPosition()
    {
        if (SetArray[0].name == "D" && intermediateNextPosition != null)
        {
            agent.destination = intermediateNextPosition.position;
        }
        else if (SetArray[0].name == "E" && intermediateNextPosition != null)
        {
            agent.destination = intermediateNextPosition.position;
        }

    }

    private void UpdateIntermediateNextPosition1()
    {
        if (SetArray[0].name == "D" && intermediateNextPosition1 != null)
        {
            agent.destination = intermediateNextPosition1.position;
        }
        else if (SetArray[0].name == "E" && intermediateNextPosition1 != null)
        {
            agent.destination = intermediateNextPosition1.position;
        }
    }

    private void SelectSpecifiedPosition()
    {
        string specifiedName = specifiedNames[indexSP];

        foreach (var kvp in destinationArrays)
        {
            for (int i = 0; i < kvp.Value.Length; i++)
            {
                if (kvp.Value[i].name == specifiedName)
                {
                    SpecifiedPosition = kvp.Value[i];
                    string key = kvp.Key.StartsWith("B") || kvp.Key.StartsWith("C") ? kvp.Key[0].ToString() : kvp.Key;
                    intermediatePosition = intermediateDestinations[key][i - 1];
                    intermediate2Position = intermediate2Destinations[kvp.Key][i];
                    intermediatePreviousPosition = i > 0 ? intermediateDestinations[key][i - 1] : null;

                    if (kvp.Key == "D")
                    {
                        intermediateNextPosition = BL.Length > 0 ? BL[0] : null;
                        intermediateNextPosition1 = D.Length > 5 ? D[5] : null;
                    }
                    else if (kvp.Key == "E")
                    {
                        intermediateNextPosition = CL.Length > 0 ? CL[0] : null;
                        intermediateNextPosition1 = E.Length > 5 ? E[5] : null;
                    }

                    SelectPLER(Array.IndexOf(new string[] { "A", "BL", "BR", "CL", "CR", "D", "E" }, kvp.Key) + 1, i - 1);
                    return;
                }
            }
        }

        Debug.LogError("Specified name not found in any destination sets: " + specifiedName);
    }

    private void SelectPLER(int setIndex, int index)
    {
        string[] keys = { "A", "BL", "BR", "CL", "CR", "D", "E" };
        if (setIndex >= 1 && setIndex <= keys.Length)
        {
            string key = keys[setIndex - 1];
            if (index < PLERs[key].Length)
            {
                PLER = PLERs[key][index];
                SetArray = destinationArrays[key];
            }
            else
            {
                Debug.LogError($"Index out of range for PLER{key}: {index}");
            }
        }
        else
        {
            Debug.LogError("Invalid setIndex: " + setIndex);
        }
    }

    private void RotateTowardsMovementDirection()
    {
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, 180 * Time.deltaTime);
        }
    }

    private bool ShouldRotateTowardsMovementDirection()
    {
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;
        return Vector3.Dot(transform.forward, direction) < 0.95f;
    }

    private void StartRotation()
    {
        isRotating = true;
        rotationStartTime = Time.time;
        agent.isStopped = true;
    }

    private IEnumerator DelayCoroutine(float delay, bool setPlerShouldFollowTker = false)
    {
        isWaiting = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(delay);
        agent.isStopped = false;
        if (setPlerShouldFollowTker)
        {
            delayCoroutineCallCount++;
            if (delayCoroutineCallCount == 2)
            {
                plerShouldFollowTker = false;
                plerWaitingForTker = true;
                delayCoroutineCallCount = 0;
            }
            else
            {
                plerShouldFollowTker = true;
            }
        }
        isWaiting = false;
        hasReachedDestination = true;
        SetNextDestination();
    }
}