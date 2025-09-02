/*using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.ComponentModel;


public class NavTestScript : MonoBehaviour
{
    private NavMeshAgent navAgent;
    public Transform dest;
    public GameObject spawnPoint;
    public GameObject playSpot;
    private GameObject mgmtObj;
    private UnityEngine.Component gameManager;
    public bool inQueue;

    private float wanderTimer = 45.0f;
    private float moveTimer;
    private float wanderRadius;

    public bool atSpot;

    void Start()
    {
        mgmtObj = GameObject.Find("SlotMachine");
        navAgent = GetComponent<NavMeshAgent>();
        gameManager = mgmtObj.GetComponent("GameManager");
        inQueue = true;
        StartCoroutine(Wandering());
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position == playSpot.transform.position)
        {
            atSpot = true;
        }
        else atSpot = false;
        if(Vector3.Distance(dest.position, gameObject.transform.position) > 5.0f)
        {
            agent.destination = dest.position;
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            StopCoroutine(Wandering());
            Debug.Log("U key pressed. Not in queue");
            inQueue = false;
            navAgent.destination = playSpot.transform.position;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("Y key pressed. Now in queue");
            inQueue = true;
            StartCoroutine(Wandering());
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist = 15.0f, int layermask = 3)
    {
        Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }


    IEnumerator Wandering()
    {
        while (inQueue)
        { 
            Vector3 newPos = RandomNavSphere(transform.position);
            navAgent.SetDestination(newPos);
            yield return new WaitForSeconds(5);
        }
    }
} */
