using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Data.Player;
using NPC;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
//using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;
using Image = UnityEngine.UI.Image;

namespace Data.GameManager
{
    public class GameManager : MonoBehaviour
    {
        /***********************************************************************************************************************
         *** CLASS MEMBERS
         ***********************************************************************************************************************/
        [Header("Predefined World Information")]
        public AudioSource playerAudioMusic;
        public AudioSource playerAudioAmbient;

        public AudioSource playerAudioCenter;
        public AudioSource playerAudioLeft;
        public AudioSource playerAudioRight;
        public AudioSource patronAudio;
        public GameObject spawnPoint;
        public GameObject playPoint;
        public GameObject exitDoor;
        public List<GameObject> patronArchetypes;
        public int patronsToSpawn = 2;

        [Header("Game Participants")]
        public SlotMachine slotMachine;
        public List<GameObject> patronQueue;
        public GameObject activePatronObject;
        public Patron activePatronScript;

        [Header("Game State")]
        public long nextBet;
        public long nextMaintenanceCost = 1000;
        public long levelGoal = 15000;
        public float maintenanceTimeWindowSeconds = 60.0f;
        public float maintenanceTimeLeftSeconds = 0.0f;

        [Header("UI Components")]
        public TextMeshProUGUI patronName;
        public TextMeshProUGUI playerMoney;
        public TextMeshProUGUI patronMoney;
        public TextMeshProUGUI maintenanceTimer;
        public TextMeshProUGUI maintenanceFee;
        public TextMeshProUGUI patronCount;
        public TextMeshProUGUI patronStress;
        public TextMeshProUGUI patronMinMoney;
        public TextMeshProUGUI patronMaxWinnings;
        public TextMeshProUGUI patronCurrentWinnings;
        public TextMeshProUGUI patronMaxLoss;
        public TextMeshProUGUI patronCurrentLoss;
        public TextMeshProUGUI patronMaxStress;

        public GameObject pauseMenu;
        public GameObject symbolInfoMenu;
        public GameObject patronInfoMenu;
        public bool gamePaused;

        private bool m_devControls = false;


        /***********************************************************************************************************************
         *** UNITY IMPLEMENTATIONS
         ***********************************************************************************************************************/
        private void Start()
        {
            pauseMenu.gameObject.SetActive(false);


            /*********************************
             *** Ambient Audio
             *********************************/

            GameObject playerAudioObjectMusic = GameObject.Find("PlayerAudioMusic");
            if (!playerAudioObjectMusic)
            {
                throw new NullReferenceException("PlayerAudioMusic not found by GameManager.");
            }

            playerAudioMusic = playerAudioObjectMusic.GetComponent<AudioSource>();

            GameObject playerAudioObjectAmbient = GameObject.Find("BackgroundAudio");
            if (!playerAudioObjectAmbient)
            {
                throw new NullReferenceException("BackgroundAudio not found by GameManager.");
            }

            playerAudioAmbient = playerAudioObjectAmbient.GetComponent<AudioSource>();

            /*********************************
             *** Player Audio
             *********************************/
            GameObject playerAudioObjectCenter = GameObject.Find("PlayerAudioCenter");
            if (!playerAudioObjectCenter)
            {
                throw new NullReferenceException("PlayerAudioCenter not found by GameManager.");
            }

            playerAudioCenter = playerAudioObjectCenter.GetComponent<AudioSource>();

            GameObject playerAudioObjectLeft = GameObject.Find("PlayerAudioLeft");
            if (!playerAudioObjectLeft)
            {
                throw new NullReferenceException("PlayerAudioLeft not found by GameManager.");
            }

            playerAudioLeft = playerAudioObjectLeft.GetComponent<AudioSource>();

            GameObject playerAudioObjectRight = GameObject.Find("PlayerAudioRight");
            if (!playerAudioObjectRight)
            {
                throw new NullReferenceException("PlayerAudioRight not found by GameManager.");
            }

            playerAudioRight = playerAudioObjectRight.GetComponent<AudioSource>();

            GameObject patronAudioObject = GameObject.Find("PatronAudio");
            if (!patronAudioObject)
            {
                throw new NullReferenceException("PatronAudio not found by GameManager.");
            }

            /*********************************
             *** Patron Audio
             *********************************/
            patronAudio = patronAudioObject.GetComponent<AudioSource>();


            /*********************************
             *** Other objects
             *********************************/
            playPoint = GameObject.Find("PlayPoint");
            exitDoor = GameObject.Find("ExitDoor");
            SlotMachine slotMachine;
            if (!playPoint)
            {
                throw new NullReferenceException("PlayPoint not found by GameManager.");
            }

            spawnPoint = GameObject.Find("SpawnPoint");
            if (!spawnPoint)
            {
                throw new NullReferenceException("SpawnPoint not found by GameManager.");
            }

            SlotMachine slotMachineScript = GetComponent<SlotMachine>();
            if (!slotMachineScript)
            {
                throw new NullReferenceException("SlotMachine script not found by GameManager.");
            }

            slotMachine = slotMachineScript;

            PatronArchetypeList patronArchetypeList = GetComponent<PatronArchetypeList>();
            if (!patronArchetypeList)
            {
                throw new NullReferenceException("PatronArchetypeList script not found by GameManager.");
            }

            /*********************************
             *** Start game
             *********************************/
            playerAudioAmbient.loop = true;
            playerAudioAmbient.clip = Resources.Load<AudioClip>("Audio/SFX_Ambience");
            playerAudioAmbient.Play();

            playerAudioMusic.loop = true;
            playerAudioMusic.clip = Resources.Load<AudioClip>("Audio/Music_Casino_1");
            playerAudioMusic.Play();

            patronArchetypes = patronArchetypeList.m_PatronArchetypes;

            maintenanceTimeLeftSeconds = maintenanceTimeWindowSeconds;

            GeneratePatrons(patronsToSpawn);
        }

        private void Update()
        {
            if (gamePaused)
            {
                Time.timeScale = 0f;
                patronAudio.Pause();
                playerAudioLeft.Pause();
                playerAudioRight.Pause();
                playerAudioCenter.Pause();
            }
            if (!gamePaused)
            {
                Time.timeScale = 1f;
                patronAudio.UnPause();
                playerAudioLeft.UnPause();
                playerAudioRight.UnPause();
                playerAudioCenter.UnPause();
            }

            UpdateHUD();

            if (slotMachine.playerMoney < 0)
            {
                ReturnToStart();
            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                m_devControls = !m_devControls;
            }

            if (!activePatronObject && Input.GetKeyDown(KeyCode.Space))
            {
                GetNextPatron();
            }

            if (m_devControls && Input.GetKeyDown(KeyCode.Q))
            {
                GeneratePatrons(patronsToSpawn);
            }

            if (activePatronScript)
            {
                if (activePatronScript.PatronReturnsToQueue(nextBet))
                {
                    ReturnPatronToQueue();
                }

                if (activePatronScript.PatronLeaves(nextBet))
                {
                    patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Loss"));
                    RemovePatronFromCasino();
                }
            }

            if (activePatronObject)
            {
                if (activePatronScript.PatronShouldPullLever() && !slotMachine.IsAnyWheelSpinning())
                {
                    patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_InsertCash"));
                    Debug.Log("GameManager::Update - Patron is pulling lever");
                    playerAudioRight.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Lever"));
                    Animator patronSlotPull = activePatronObject.GetComponentInChildren<Animator>();

                    if (patronSlotPull)
                    {
                        patronSlotPull.SetTrigger("PullLever");
                        patronSlotPull.ResetTrigger("PullLever");
                    }

                    activePatronScript.currentMoney -= nextBet;
                    slotMachine.playerMoney += nextBet;

                    slotMachine.PullLever();
                    patronAudio.loop = true;
                    int randval = Random.Range(0, 1);

                    if (randval == 0)
                    {
                        patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Spinning_1"));
                    } else
                    {
                        patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Spinning_2"));
                    }

                    activePatronScript.waitingForResults = true;
                }


                if (!slotMachine.IsAnyWheelSpinning() && activePatronScript.waitingForResults)
                {
                    patronAudio.Stop();
                    slotMachine.GetSlotsResult(out SlotsResult result);

                    if (result.m_isJackpot)
                    {
                        patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Result_Jackpot"));
                    } else if (result.m_isCombo)
                    {
                        patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Result_Combo"));
                    } else
                    {
                        patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_Result_Base"));
                    }

                    slotMachine.playerMoney -= result.m_patronMoneyAdjustment;

                    Debug.Log(result.ToString());
                    
                    activePatronScript.UpdatePatronsState(result.m_patronMoneyAdjustment,
                        result.m_patronStressAdjustment,
                        result.m_finalWheelLockTime);
                    activePatronScript.waitingForResults = false;
                }
            }

            maintenanceTimeLeftSeconds -= Time.deltaTime;
            if (maintenanceTimeLeftSeconds < 0.0f)
            {
                maintenanceTimeLeftSeconds = maintenanceTimeWindowSeconds;
                slotMachine.playerMoney -= nextMaintenanceCost;
                nextMaintenanceCost = (long)(nextMaintenanceCost * 1.1f);
            }
        }
        /***********************************************************************************************************************
         *** PUBLIC METHODS
         ***********************************************************************************************************************/

        /***********************************************************************************************************************
        *** Level Handler
        ***********************************************************************************************************************/

        public void LevelComplete()
        {
            if (slotMachine.playerMoney >= levelGoal)
            {
                SceneManager.LoadScene("StartMenu");
            }
        }
        /***********************************************************************************************************************
         *** PRIVATE METHODS
         ***********************************************************************************************************************/
        private void GeneratePatrons(int numPatrons)
        {
            for (int i = 0; i < numPatrons; i++)
            {
                int randPatron = Random.Range(0, patronArchetypes.Count);
                patronQueue.Add(Instantiate(patronArchetypes[randPatron], new Vector3(17.5f, 1.29f, 5.5f), Quaternion.identity));
            }

            Debug.Log("GameManager::GeneratePatrons - Patrons generated!");
        }

        private void RemovePatronFromCasino()
        {
            Debug.Log("GameManager::RemovePatronFromCasino - Patron left the casino!");
            //DestroyImmediate(activePatronObject);
            activePatronObject = null;
            activePatronScript = null;
            GetNextPatron();
        }

        private void ReturnPatronToQueue()
        {
            Debug.Log("GameManager::ReturnPatronToQueue - Patron returning to queue!");
            patronQueue.Add(activePatronObject);
            activePatronObject = null;
            activePatronScript = null;
            GetNextPatron();
        }

        private void GetNextPatron()
        {
            if (patronQueue.Count <= 0)
            {
                ReturnToStart();
            }

            Debug.Log("GameManager::GetNextPatron - Getting next patron!");
            if (activePatronObject)
            {
                Debug.LogError("GameManager::GetNextPatron - Patron already active!");
                return;
            }

            activePatronObject = patronQueue[0];
            Patron patronScript = activePatronObject.GetComponent<Patron>();
            patronQueue.RemoveAt(0);

            if (patronScript)
            {
                activePatronScript = patronScript;
            }

            nextBet = patronScript.nextBet;

            patronAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_New_Patron"));
            activePatronScript.GetNextSpinTime();
            activePatronScript.PatronCalled();
        }

        /***********************************************************************************************************************
         *** UI Handler
         ***********************************************************************************************************************/

        public void UpdateHUD()
        {

            if (activePatronObject)
            {
                patronName.text = (string)activePatronScript.archetype.name;  //Adding names list later that will add names to a list of active patrons, and if name already exists it will choose another from list.
                playerMoney.text = "" + slotMachine.playerMoney;
                maintenanceTimer.text = "" + Math.Floor(maintenanceTimeLeftSeconds);
                maintenanceFee.text = "" + nextMaintenanceCost;
                patronCount.text = "" + patronQueue.Count;
                patronMoney.text = "" + activePatronScript.currentMoney;
                patronStress.text = "" + activePatronScript.currentStress;
                patronMinMoney.text = "" + activePatronScript.minMoneyThreshold;
                patronMaxWinnings.text = "" + activePatronScript.winningsThreshold;
                patronCurrentWinnings.text = "" + activePatronScript.currentMoneyGain;
                patronMaxLoss.text = "" + activePatronScript.lossThreshold;
                patronCurrentLoss.text = "" + activePatronScript.currentMoneyLoss;
                patronMaxStress.text = "" + activePatronScript.stressLeaveLimit;
            }
        }

        public void PauseGame()
        {
            pauseMenu.gameObject.SetActive(true);
            gamePaused = true;
        }

        public void OpenInfoMenu()
        {
            symbolInfoMenu.gameObject.SetActive(true);

        }

        public void SwitchInfoMenu()
        {
            bool symbolInfoActive = symbolInfoMenu.activeSelf;
            bool toggleSymbolInfo = !symbolInfoActive;
            symbolInfoMenu.SetActive(toggleSymbolInfo);

            bool patronInfoActive = patronInfoMenu.activeSelf;
            bool togglePatronInfo = !patronInfoActive;
            patronInfoMenu.SetActive(togglePatronInfo);
        }

        public void BackToPauseMain()
        {
            patronInfoMenu.SetActive(false);
            symbolInfoMenu.SetActive(false);
            pauseMenu.SetActive(false);
        }

        public void ResumeGame()
        {
            patronInfoMenu.gameObject.SetActive(false);
            symbolInfoMenu.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            gamePaused = false;
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void ReturnToStart()
        {
            SceneManager.LoadScene("StartMenu");
        }
    }
}