using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Cryptography;
using Player;
using Data.NPC;
using Data.Player;
using Data.GameManager;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using System.Diagnostics;

namespace NPC
{
    public class Patron : MonoBehaviour
    {
        /***********************************************************************************************************************
         *** CLASS MEMBERS
         ***********************************************************************************************************************/
        public PatronArchetype archetype;
        private GameObject gameManagerObj;
        private GameManager gameManager;
        public SlotMachine slotMachine;
        private bool isActive;


        /*******************
         * Random Stats
         *******************/
        [Header("Winnings")]
        public long winningsThreshold;
        public long lossThreshold;
        public long minMoneyThreshold;
        public long currentMoney;

        public long currentMoneyLoss;
        public long currentMoneyGain;
        public long nextBet = 50;


        [Header("Stress")]
        public int currentStress;

        public int stressLeaveLimit;
        public int stressQuitLimit;
        public double stressMultiplier;
        public double stressTimeMultiplier;

        [Header("Spinning")]
        public bool waitingForResults;

        public int nextSpinWaitMillis;
        public string patronLastSpinTime;
        public string patronNextSpinTime;

        private DateTime m_lastSpinBacker = DateTime.MinValue;

        private DateTime m_nextSpinBacker = DateTime.MaxValue;

        private DateTime LastSpin
        {
            get
            {
                return m_lastSpinBacker;
            }
            set
            {
                m_lastSpinBacker = value;
                patronLastSpinTime = value.ToString("O");
            }
        }

        private DateTime NextSpin
        {
            get
            {
                return m_nextSpinBacker;
            }
            set
            {
                m_nextSpinBacker = value;
                patronNextSpinTime = value.ToString("O");
            }
        }
        /*******************
         * Animator & Movement components
         *******************/
        private SkinnedMeshRenderer m_meshRenderer;
        private Animator animator;

        public GameObject playSpot;
        private GameObject exitDoor;
        private NavMeshAgent navAgent;
        private int playAreaMask;
        private int casinoFloorMask;
        public float wanderRadius = 45.0f;
        public float wanderTimer;
        public float moveTimer;
        public bool inQueue;
        /***********************************************************************************************************************
         *** UNITY IMPLEMENTATIONS
         ***********************************************************************************************************************/
        private float playSpotDistance;

        private void Start()
        {
            //navAgent.autoTraverseOffMeshLink = true;
            int casinoFloorMask = NavMesh.GetAreaFromName("Casino Floor");
            int playAreaMask = NavMesh.GetAreaFromName("Play Area");

            isActive = false;
            if (archetype == null)
            {
                throw new NullReferenceException("Patrons cannot be null!");
            }

            currentMoney = archetype.GetStartingMoney();
            winningsThreshold = (long)(currentMoney * archetype.GetWinningsThresholdMult());
            lossThreshold = (long)archetype.GetLossThresholdMult();
            minMoneyThreshold = (long)archetype.GetMinMoneyThreshold();
            currentMoneyLoss = (long)0;
            currentMoneyGain = (long)0; 

            currentStress = archetype.GetStartingStress();
            stressQuitLimit = archetype.GetStressQuitLimit();
            stressLeaveLimit = archetype.GetStressLeaveLimit();
            stressMultiplier = archetype.GetStressMultiplier();
            stressTimeMultiplier = 1.0f + archetype.stressTimeMultiplier;

            animator = gameObject.GetComponent<Animator>();
            playSpot = GameObject.FindWithTag("playSpot");
            exitDoor = GameObject.FindWithTag("exitDoor");
            

            m_meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            navAgent = GetComponentInChildren<NavMeshAgent>();
            inQueue = true;
            StartCoroutine(Wandering());

            gameManagerObj = GameObject.Find("SlotMachine");
            gameManager = gameManagerObj.GetComponent<GameManager>();

            SlotMachine slotMachineScript = gameManagerObj.GetComponent<SlotMachine>();
            if (!slotMachineScript)
            {
                throw new NullReferenceException("SlotMachine script not found by GameManager.");
            }

            slotMachine = slotMachineScript;
        }

     

        private void Update()
        {
            playSpotDistance = Vector3.Distance(gameObject.transform.position, playSpot.transform.position);
        }


        /***********************************************************************************************************************
         *** PUBLIC METHODS: PATRON RESULTS
         ***********************************************************************************************************************/
        /// <summary>
        ///     Updates the patron's current state after all wheels have been locked
        /// </summary>
        /// <param name="moneyAdjustment">Money scalar adjustment</param>
        /// <param name="stressAdjustment">Stress scalar adjustment</param>
        /// <param name="finalSlotLockedTime">Time it took to finally lock slots</param>
        public void UpdatePatronsState(
            long moneyAdjustment,
            double stressAdjustment,
            DateTime finalSlotLockedTime
        )
        {
            if (playSpotDistance <= 1.0f)
            {
                GetNextSpinTime();
            }
            currentMoney += moneyAdjustment;

            // Invert time multiplier for positive stress, as we want the longer the player to reduce stress less
            //TODO: Determine how this relationship should work for positive and negative stresses, flat mult might not work.
            double stressTimeMult = stressAdjustment > 0
                                        ? stressTimeMultiplier
                                        : 1.0 / stressTimeMultiplier;

            //TODO: Determine if this relationship makes sense, adjusting the stress based on the ratio of the time the player took to spin
            // vs how long the patron would've taken to spin. Might be too sensitive.
            double stressTimeAdjustment = stressTimeMult * (finalSlotLockedTime - LastSpin).Milliseconds / nextSpinWaitMillis;

            currentStress += (int)((stressAdjustment + stressTimeAdjustment)
                                   * stressMultiplier);

            if (moneyAdjustment < 0)
            {
                currentMoneyLoss += moneyAdjustment;
            }

            if (moneyAdjustment > 0)
            {
                currentMoneyGain += moneyAdjustment;
            }
        }

        /***********************************************************************************************************************
         *** PUBLIC METHODS: PATRON ACTIONS
         ***********************************************************************************************************************/
        /// <summary>
        ///     Determines whether or not the patron will pull the lever, and updates the last time the lever was spun to now
        /// </summary>
        /// 

        //adds funtion for wandering with randomg stops.
        public static Vector3 RandomNavSphere(Vector3 origin, float dist = 15.0f, int layermask = 1)
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
                Vector3 nextPos = RandomNavSphere(transform.position);
                Vector3 newPos = nextPos;
                if (Vector3.Distance(transform.position, newPos) > navAgent.stoppingDistance)
                {
                    navAgent.SetDestination(newPos);
                }
                if (Vector3.Distance(transform.position, newPos) <= navAgent.stoppingDistance)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(3, 7));
                }
            }
        }

        IEnumerator NormalSpeed(NavMeshAgent agent)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
            while (agent.transform.position != endPos)
            {
                agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
                yield return null;
            }
        }

        public bool CrossMeshLink()
        {
            bool crossingLink = false;

            if (navAgent.isOnOffMeshLink && isActive)
            {
                crossingLink = true;
            }

            if (crossingLink)
            {
                navAgent.autoTraverseOffMeshLink = true;
                StartCoroutine(NormalSpeed(navAgent));
                navAgent.CompleteOffMeshLink();
            }

            return crossingLink;
        }


        /// <returns>True if patron should pull the lever</returns>
        public bool PatronShouldPullLever()
        {
            DateTime now = DateTime.Now;
            if ((now >= NextSpin) && !waitingForResults && (playSpotDistance <= 1.0f) && isActive)
            {
                LastSpin = now;
                animator.SetTrigger("pullLever");
                animator.ResetTrigger("pullLever");
                return true;
            }

            else return false;
        }

        /// <summary>
        ///     Determines whether the patron should stop playing and return to the queue
        /// </summary>
        /// <param name="nextBet">The next bet that would need to be placed by the patron</param>
        /// <returns>True if the patron should return to queue</returns>
        public bool PatronReturnsToQueue(long nextBet)
        {
            bool shouldReturn = false;
            if ((currentStress >= stressQuitLimit) && (currentStress <= stressLeaveLimit) && (currentMoney > nextBet) && (currentMoney > minMoneyThreshold) && !waitingForResults)
            {
                Debug.Log("Patron too stressed out.");
                shouldReturn = true;
                currentStress = (int)(currentStress * .75f);
            }

            /*if ((currentMoney < nextBet) && (currentMoneyLoss > lossThreshold) && (currentMoneyGain < winningsThreshold) && !waitingForResults)
            {
                // Patron quits for the day because they can't afford the next bet. Checks for end of time/level. Calls new patron/ends level/e.t.c.
                shouldReturn = true;
                Debug.Log("Patron can't afford next bet. Finding other games to play.");
            }*/

            if (shouldReturn)
            {
                Debug.Log("Patron returns to queue");
                //animator.SetTrigger("backToQueue");
                animator.ResetTrigger("backToQueue");
                inQueue = true;
                isActive = false;
                StartCoroutine(Wandering());
                navAgent.areaMask &= ~(1 << NavMesh.GetAreaFromName("Play Area"));
                navAgent.areaMask |= (1 << NavMesh.GetAreaFromName("Casino Floor"));
            }

            return shouldReturn;
        }

        /// <summary>
        ///     Determines if the patron should leave the casino entirely
        /// </summary>
        /// <returns>True if patron should leave</returns>
        public bool PatronLeaves(long nextBet)
        {
            //This needs to be modified to check for money vs bet amounts. Possibly money vs stress level for complexity.
            //these if's can be split down further to know what sound/visual to play depending on if patron or player is happy.
            bool shouldLeave = false;
            if ((currentMoneyGain >= winningsThreshold) && !waitingForResults)
            {
                // Patron leaves. Checks for end of time/level. Calls new patron/ends level/e.t.c.?
                // TODO: Trigger out of money effects
                Debug.Log("Patron leaves happily with their winnings.");
                shouldLeave = true;
            }

            if ((currentStress >= stressLeaveLimit) && !waitingForResults)
            {
                // TODO: Trigger too stressed effects
                Debug.Log("Patron got too stressed and decided to leave.");
                shouldLeave = true;
            }

            if ((currentMoney <= minMoneyThreshold) && !waitingForResults)
            {
                Debug.Log("Patron sees how little they have and leave.");
                shouldLeave = true;
            }

            if ((currentMoneyLoss <= lossThreshold) && !waitingForResults)
            {
                Debug.Log("Patron has lost too much money today. They decide to leave.");
                shouldLeave = true;
            }

            if ((currentMoney < nextBet) && !waitingForResults)
            {
                // Patron quits for the day because they can't afford the next bet. Checks for end of time/level. Calls new patron/ends level/e.t.c.
                shouldLeave = true;
                Debug.Log("Patron can't afford their next bet.");
            }

            if ((currentMoney <= 0) && !waitingForResults)
            {
                Debug.Log("Player has no money left.");
                shouldLeave = true;
            }

            if (shouldLeave)
            {
                Debug.Log("Patron should leave anim");
                //animator.SetTrigger("patronLeaves");
                //animator.SetBool("playingGame", false);
                navAgent.destination = exitDoor.transform.position;
                isActive = false;
                navAgent.areaMask &= ~(1 << NavMesh.GetAreaFromName("Play Area"));
                navAgent.areaMask |= (1 << NavMesh.GetAreaFromName("Casino Floor"));
                //Destroy(gameObject);
                //animator.ResetTrigger("patronLeaves");
            }

            return shouldLeave;
        }

        /// <summary>
        ///     Sets the next randomized time the patron should spin based on the archetype limits
        /// </summary>
        public void GetNextSpinTime()
        {
            nextSpinWaitMillis = archetype.GetTimeToSpinMillis();
            NextSpin = DateTime.Now + TimeSpan.FromMilliseconds(nextSpinWaitMillis);
        }

        /***********************************************************************************************************************
         *** PRIVATE METHODS
         ***********************************************************************************************************************/

        public void PatronCalled()
        {
            inQueue = false;
            isActive = true;

            //animator.SetTrigger("patronCalled");
            //animator.ResetTrigger("patronCalled");
            //animator.SetBool("playingGame", true);

            StopCoroutine(Wandering());
            navAgent.destination = playSpot.transform.position;
            if (Vector3.Distance(transform.position, playSpot.transform.position) <= 3.0f)
            {
                //transform.LookAt(slotMachine.transform.position);
            }
            navAgent.areaMask = NavMesh.AllAreas;
            navAgent.areaMask &= ~(1 << NavMesh.GetAreaFromName("Casino Floor"));
            Debug.Log("Next patron on their way to play.");
        }

        public void OnTriggerEnter(Collider other)
        {
            if ((other.gameObject == exitDoor))
            {
                Destroy(this.gameObject);
            }
        }
    }
}
