using System;
using System.Collections.Generic;
using System.Linq;
using Data.Player;
using Data.GameManager;
using Enum;
using Finders;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Debug = UnityEngine.Debug;
using System.Security.Permissions;

namespace Player
{
    /// <summary>
    ///     The main player class for the game, responsible for holding the player's state
    /// </summary>
    [Serializable]
    public class SlotMachine : MonoBehaviour
    {
        /***********************************************************************************************************************
         *** CLASS MEMBERS
         ***********************************************************************************************************************/
        [Header("Wheel Stats")]
        public GameObject wheelPrefab;
        public int maxWheels = 5;
        public int startingWheels = 3;
        public List<GameObject> slotWheels = new List<GameObject>();
        public List<SlotWheel> slotWheelScripts = new List<SlotWheel>();

        [Header("Results UI")]
        public Sprite currentWheelSprite;

        public GameObject slotSymbolUI;
        public GameObject resultUIContainer;
        public List<GameObject> resultUIContainerList = new List<GameObject>();
        public GameObject slotSymbolUIImgSlot;
        public Image slotSymbolUIImage;

        public GameManager GameManager;


        [Header("Wheel Spawning")]
        public float workableContainerScale = 0.95f;
        public float wheelSpacingScale = 0.1f;

        [Header("Spinning Stats")]
        public int currentWheel;

        [Header("Player Stats")]
        public long playerMoney = 10000;

        [System.Serializable]
        public struct Inventory
        {
            public int powerupsPurchased;
            public List<PowerUpItem> powerups;

            public List<float> totalSymbolMoneyModifier;
            public List<float> totalSymbolStressModifier;
            public List<float> totalSymbolProbabilityModifier;
            public List <float> totalSymbolComboModifier;
        }
        public Inventory inventory;

        [Header("Slot Timings")]
        public double maxTimeToLockAllSlots;
        public string leverPullTime;
        public string finalWheelStopTime;

        private DateTime _machineFinalWheelStopTimeBacker;
        private DateTime _machineLeverPullTimeBacker;

        private SpindleFinder _spindle;
        private MeshRenderer _spindleMesh;
        private float _spindleWidth;
        private float _workableSpindleWidth;

        private bool _devControls = false;
        public DateTime MachineLeverPullTime
        {
            get
            {
                return _machineLeverPullTimeBacker;
            }

            private set
            {
                _machineLeverPullTimeBacker = value;
                leverPullTime = value.ToString("O");
            }
        }

        public DateTime MachineFinalWheelStopTime
        {
            get
            {
                return _machineFinalWheelStopTimeBacker;
            }

            private set
            {
                _machineFinalWheelStopTimeBacker = value;
                finalWheelStopTime = value.ToString("O");
            }
        }


        /***********************************************************************************************************************
         *** UNITY IMPLEMENTATIONS
         ***********************************************************************************************************************/
        private void Start()
        {
            SpindleFinder spindleFinder = GetComponentInChildren<SpindleFinder>();
            if (!spindleFinder)
            {
                throw new Exception("Slot machine is missing a spindle.");
            }

            _spindle = spindleFinder;
            
            _spindleMesh = _spindle.gameObject.GetComponent<MeshRenderer>();
            if (!_spindleMesh)
            {
                throw new Exception("Spindle is missing mesh.");
            }

            _spindleWidth = _spindleMesh.bounds.size.x;
            _workableSpindleWidth = _spindleWidth * workableContainerScale;

            for (int i = 0; i < startingWheels; i++)
            {
                AddNewWheel();
            }

            MachineLeverPullTime = DateTime.MaxValue;
            MachineFinalWheelStopTime = DateTime.MinValue;

            
            currentWheel = 0;

            //this instantiates the results ui and creates a list to access in associating each wheel with a different result gameobject
            //The partner to this is in the StopWheel function
            foreach (SlotWheel slotWheelScript in slotWheelScripts)
            {
                GameObject newContainer = Instantiate(resultUIContainer, slotSymbolUI.transform);
                resultUIContainerList.Add(newContainer);
            }

            // Initialize inventory
            // TODO: Update these when player gets a powerup.
            // We should prolly have an inventory script that the player owns that handles the logic here. 
            inventory.totalSymbolStressModifier = Enumerable.Repeat(0.0f, (int)SlotSymbolEnum.NUM_SYMBOLS).ToList();
            inventory.totalSymbolMoneyModifier = Enumerable.Repeat(0.0f, (int)SlotSymbolEnum.NUM_SYMBOLS).ToList();
            inventory.totalSymbolComboModifier = Enumerable.Repeat(1.0f, (int)SlotSymbolEnum.NUM_SYMBOLS).ToList();
            inventory.totalSymbolProbabilityModifier = Enumerable.Repeat(0.0f, (int)SlotSymbolEnum.NUM_SYMBOLS).ToList();
        }

        private void Update()
        {
            if (IsAnyWheelSpinning() && Input.GetMouseButtonDown(0))
            {
                StopNextWheel();

            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                _devControls = !_devControls;
            }

            if (_devControls)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    AddNewWheel();
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    RemoveWheelLeft();
                }

                if (Input.GetKeyDown(KeyCode.D))
                {
                    RemoveWheelRight();
                }
            }

        }

        /***********************************************************************************************************************
         *** PUBLIC METHODS: ADDING/REMOVING WHEELS
         ***********************************************************************************************************************/

        /// <summary>
        ///     Attempts to add a new fresh wheel to the end slot machine if under the max number of wheels
        /// </summary>
        public void AddNewWheel()
        {
            UpdateWheel(Instantiate(wheelPrefab, _spindle.transform), slotWheels.Count);
        }

        /// <summary>
        ///     Attempts to add a specific wheel to the end of the slot machine if under the max number of wheels
        /// </summary>
        /// <param name="wheelToAdd">Specific wheel to add to the machine</param>
        public void AddWheel(GameObject wheelToAdd)
        {
            UpdateWheel(wheelToAdd, slotWheels.Count);
        }

        /// <summary>
        ///     <c>UpdateWheel</c> overwrites a specified slot with the passed in slot
        /// </summary>
        /// <param name="wheelToAdd">New slot to use</param>
        /// <param name="slotIndex">Slot to replace</param>
        /// <exception cref="ArgumentException">Thrown if slot is invalid or slot index is out of bounds</exception>
        public void UpdateWheel(in GameObject wheelToAdd, int slotIndex)
        {
            if (!wheelToAdd)
            {
                Debug.LogError("SlotMachine::UpdateWheel - WheelToAdd is null");
                return;
            }

            bool error = false;
            if ((slotIndex < 0) || (slotIndex > maxWheels))
            {
                error = true;
                Debug.LogError("SlotMachine::UpdateWheel - Invalid slot index");
            }

            if ((slotWheels.Count >= maxWheels) && (slotIndex > (maxWheels - 1)))
            {
                error = true;
                Debug.LogError("SlotMachine::UpdateWheel - Cannot add more slots");
            }

            if (IsAnyWheelSpinning())
            {
                error = true;
                Debug.LogError("SlotMachine::UpdateWheel - Cannot add wheel while in play");
            }

            SlotWheel slotWheelScript = wheelToAdd.GetComponent<SlotWheel>();

            if (!slotWheelScript.IsValid())
            {
                error = true;
                Debug.LogError("SlotMachine::UpdateWheel - Invalid slot wheel");
            }

            if (error)
            {
                DestroyImmediate(wheelToAdd);
                return;
            }

            UpdateAndResizeWheels(wheelToAdd, slotIndex);
        }

        /// <summary>
        ///     Shortcut method for removing the rightmost wheel from the machine
        /// </summary>
        public void RemoveWheelRight()
        {
            RemoveWheel(slotWheels.Count - 1);
        }

        /// <summary>
        ///     Shortcut method for removing the leftmost wheel from the machine
        /// </summary>
        public void RemoveWheelLeft()
        {
            RemoveWheel(0);
        }

        /// <summary>
        ///     Attempts to remove the wheel at the specified index from the machine
        /// </summary>
        /// <param name="wheelIndex">Index of the wheel to be removed</param>
        public void RemoveWheel(int wheelIndex)
        {
            if (IsAnyWheelSpinning())
            {
                Debug.LogError("SlotMachine::RemoveWheel - Cannot remove a slot during play!");
                return;
            }

            if (slotWheels.Count == 0)
            {
                Debug.LogError("SlotMachine::RemoveWheel - No slot wheels to remove. How did you do this?! >:(");
                return;
            }

            if (slotWheels.Count == 1)
            {
                Debug.LogError("SlotMachine::RemoveWheel - Must have at least one wheel!");
                return;
            }

            if ((wheelIndex > (slotWheels.Count - 1)) || (wheelIndex < 0))
            {
                Debug.LogError("SlotMachine::RemoveWheel - Invalid slot wheel index");
                return;
            }

            GameObject wheelToRemove = slotWheels[wheelIndex];
            slotWheels.RemoveAt(wheelIndex);
            DestroyImmediate(wheelToRemove);
            if (slotWheels.Count > 0)
            {
                UpdateAndResizeWheels(slotWheels[0],
                    0,
                    true);
            }
        }

        /***********************************************************************************************************************
         *** PUBLIC METHODS: SPINNING AND STOPPING WHEELS
         ***********************************************************************************************************************/
        /// <summary>
        ///     Sets the time the lever was pulled by the patron
        /// </summary>
        public void PullLever()
        {
            MachineLeverPullTime = DateTime.Now;
            foreach (SlotWheel slotWheelScript in slotWheelScripts)
            {
                slotWheelScript.StartSpinning(); 
            }
            foreach (GameObject resultUI in resultUIContainerList)
            {
                resultUI.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = null;
            }
            currentWheel = 0;
        }

        /// <summary>
        ///     Attempts to stop the next wheel on the machine
        /// </summary>
        public void StopNextWheel()
        {

            if (currentWheel >= slotWheels.Count)
            {
                Debug.LogError("SlotMachine::StopNextWheel - Wheels are all stopped");
                return;
            }



            slotWheels[currentWheel].GetComponent<SlotWheel>().StopSpinning();

            resultUIContainerList[currentWheel].transform.GetChild(0).gameObject.GetComponent<Image>().sprite = slotWheels[currentWheel].GetComponent<SlotWheel>().currentSymbol.image;
            //Debug.Log(resultUIContainerList[currentWheel].transform.GetChild(0).gameObject.GetComponent<Image>().sprite);
            Debug.Log(slotWheelScripts[currentWheel].GetComponent<SlotWheel>().currentSymbol.image);

            currentWheel++;

            if (currentWheel >= slotWheels.Count)
            {
                MachineFinalWheelStopTime = DateTime.Now;
            }

        }

        /// <summary>
        ///     Simple check to see if any wheel is still spinning
        /// </summary>
        /// <returns>True if a wheel still spins</returns>
        public bool IsAnyWheelSpinning()
        {
            foreach (SlotWheel slotWheelScript in slotWheelScripts)
            {
                if (slotWheelScript.IsSpinning())
                {
                    return true;
                }
            }

            return false;
        }


        /***********************************************************************************************************************
         *** PUBLIC METHODS: GETTING RESULTS
         ***********************************************************************************************************************/

        /// <summary>
        ///     <c>GetSlotsResult</c> gets the current slot states and calculates the result of the spin and assigns it to the out
        ///     reference
        /// </summary>
        /// <param name="result">Reference ot the result</param>
        public void GetSlotsResult(out SlotsResult result)
        {
            /*if (currentWheel < slotWheels.Count)
            {
                throw new InvalidOperationException("SlotMachine::GetSlotsResult - Wheels are not all stopped");
            }

            currentWheel = 0;*/
            result = new SlotsResult();

            // TODO: This code doesn't work as intended. In theory you could have 5 wheels and have a combo on 4 symbols but not 1.
            // right now, it applies the combo modifier to every wheel but should only apply to those wheels with the symbol in question
            // also there's no difference between a combo and a jackpot. UNLESS we want to make it only a jackpot if the number of jackpot
            // symbols HAS to make it the same as the number of wheels.
            List<int> symbolOccurenceCount = Enumerable.Repeat(0, (int)SlotSymbolEnum.NUM_SYMBOLS)
                .ToList();
            
            foreach (SlotWheel slotWheel in slotWheelScripts)
            {
                int slotTypeIndex = (int)slotWheel.currentSymbol.type;
                
                symbolOccurenceCount[slotTypeIndex]++;
                if (symbolOccurenceCount[slotTypeIndex] >= 3)
                {
                    if (slotWheel.currentSymbol.type == SlotSymbolEnum.Jackpot)
                    {
                        result.m_isJackpot = true;
                        Debug.Log("Jackpot!!!");
                    }
                    else
                    {
                        result.m_isCombo = true;
                        Debug.Log("Combo hit!");
                    }
                }
            }

            foreach (GameObject slotWheel in slotWheels)
            {
                SlotWheel slotWheelScript = slotWheel.GetComponent<SlotWheel>();
                
                result.m_patronMoneyAdjustment += slotWheelScript.currentSymbol.script.GetPatronMoneyAdjustment(result.m_isCombo,
                    inventory.totalSymbolMoneyModifier[(int)slotWheelScript.currentSymbol.type],
                    inventory.totalSymbolStressModifier[(int)slotWheelScript.currentSymbol.type]);
                
                result.m_patronStressAdjustment += slotWheelScript.currentSymbol.script.GetPatronStressAdjustment(result.m_isCombo,
                    inventory.totalSymbolStressModifier[(int)slotWheelScript.currentSymbol.type],
                    inventory.totalSymbolStressModifier[(int)slotWheelScript.currentSymbol.type]);

                Debug.Log(result.m_patronMoneyAdjustment);
                Debug.Log(result.m_patronStressAdjustment);
            }


            result.m_finalWheelLockTime = MachineFinalWheelStopTime;
        }
      

        /***********************************************************************************************************************
         *** PRIVATE METHODS
         ***********************************************************************************************************************/

        /// <summary>
        ///     Attempts to resize the wheels so that they properly fit onto the slot machine
        /// </summary>
        /// <param name="wheelBeingAdded">Wheel that's being added to the slot machine</param>
        /// <param name="slotToUpdate">The position the wheel being added should be placed into</param>
        /// <returns>True if the wheel has the expected transform and collider, false if not</returns>
        private bool UpdateAndResizeWheels(
            GameObject wheelBeingAdded,
            int slotToUpdate,
            bool justResize = false
        )
        {
            wheelBeingAdded.transform.parent = _spindle.transform;
            
            WheelFinder wheelBeingAddedFinder = wheelBeingAdded.GetComponentInChildren<WheelFinder>();
            MeshRenderer wheelBeingAddedMesh = wheelBeingAddedFinder.GetComponentInParent<MeshRenderer>();

            if (!wheelBeingAddedMesh)
            {
                Debug.LogError("SlotMachine::UpdateAndResizeWheels - Wheel is missing a MeshRenderer component.");
                return false;
            }

            if (!justResize)
            {
                if (slotWheels.Count > 0)
                {
                    // If we already have wheels, it's a safe bet we can rely on their transforms to position the new one correctly if it isn't
                    wheelBeingAdded.transform.position = slotWheels[0].transform.position;
                    wheelBeingAdded.transform.rotation = slotWheels[0].transform.rotation;
                    wheelBeingAdded.transform.localScale = slotWheels[0].transform.localScale;
                } else
                {
                    // Otherwise we can reposition the wheel to the slot machine reel and hope for the best
                    wheelBeingAdded.transform.position = transform.position;
                    wheelBeingAdded.transform.rotation = transform.rotation;
                }

                slotWheels.Insert(slotToUpdate, wheelBeingAdded);
            }

            float wheelWidth = (1.0f - wheelSpacingScale) * (_workableSpindleWidth / slotWheels.Count);

            Vector3 wheelRescale = wheelBeingAddedMesh.transform.localScale;

            wheelRescale.x = wheelWidth * wheelRescale.x / wheelBeingAddedMesh.bounds.size.x;

            float wheelSpacing = _workableSpindleWidth / slotWheels.Count;

            // TODO: Work out a new way to get teh spindle start position. Linear interpolation works but only
            // if the spindle isn't rescaled

            // Previously this worked:
            // float startPos = -m_workableSpindleWidth / 2 + wheelSpacing * wheelSpacingScale / 2;

            // Tried random bullshit
            // float startPos = m_workableSpindleWidth / workableContainerScale * (1.0f - workableContainerScale);
            // float startPos = wheelRescale.x / 10;

            // Found good value for left bound and tried statid
            // float startPos = 0.69498f

            // Manually adjusted for all 5 slot options, then did linear fit
            // 0.69498 -> 0.69498;
            // 0.34749 -> 0.819;
            // 0.23166 -> 0.864;
            // 0.173745 -> 0.869;
            // 0.138996 -> 0.895;
            float startPos = (-0.3513535f * wheelRescale.x / 10.0f) + 0.9399065f;

            slotWheelScripts.Clear();
            for (int i = 0; i < slotWheels.Count; i++)
            {
                GameObject wheelToAdjust = slotWheels[i];

                WheelFinder wheelToAdjustFinder = wheelToAdjust.GetComponentInChildren<WheelFinder>();
                MeshRenderer wheelToAdjustMesh = wheelToAdjustFinder.GetComponentInParent<MeshRenderer>();

                wheelToAdjustMesh.transform.localScale = wheelRescale;

                Vector3 wheelMeshShift = wheelToAdjustMesh.transform.localPosition;
                wheelMeshShift.y = -(wheelRescale.x / 10);
                wheelToAdjustMesh.transform.localPosition = wheelMeshShift;

                // float startPos = 1 - ((m_workableSpindleWidth / workableContainerScale)
                //                       * ((1.0f - workableContainerScale) / 2)
                //                       + (wheelSpacingScale / 2));

                Vector3 wheelLocalPos = new Vector3(0,
                    startPos,
                    0);

                wheelToAdjust.transform.localPosition = wheelLocalPos;

                Vector3 wheelGlobalShift = wheelToAdjust.transform.position;
                wheelGlobalShift.x += wheelSpacing * i;
                wheelToAdjust.transform.position = wheelGlobalShift;

                wheelToAdjust.name = "Wheel_" + i;

                SlotWheel wheelScript = wheelToAdjust.GetComponentInChildren<SlotWheel>();
                slotWheelScripts.Add(wheelScript);

                //for each wheel we instantiate a panel in results UI. This should link each wheel to a UI panel.


            }
            
            currentWheel = slotWheels.Count + 1;

            return true;
        }
    }
}