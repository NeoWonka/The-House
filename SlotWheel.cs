using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;
using Data.Player;
using Enum;
using Finders;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Debug = UnityEngine.Debug;
using Physics = UnityEngine.Physics;
using Random = UnityEngine.Random;
using Image = UnityEngine.UI.Image;
// Debug shapecast
// using RotaryHeart.Lib.PhysicsExtension;

namespace Player
{
    public class SlotWheel : MonoBehaviour
    {
        /***********************************************************************************************************************
         *** CLASS MEMBERS
         ***********************************************************************************************************************/
        public bool isDummy;
        
        public struct Spinner
        {
            public bool isSpinning;
            public bool waitingForFinalCastHit;
            public bool waitingForFirstCastHit;

            public List<double> resultProbabilities;
            public float guaranteeNextHit;

            public Vector3 shapecastDirection;
            public Vector3 shapecastStartPos;
            public ShapecasterFinder shapecaster;
            
            public SlotSymbol lastCastHit;
            public WheelFinder wheel;
            public ArrowFinder arrow;
        }

        [Serializable]
        public struct CurrentSymbol
        {
            public SlotSymbol script;
            public SlotSymbolEnum type;
            public SlotSymbolArchetype archetype;
            public Guid id;
            public Sprite image;
            public int index;
        }

        [Header("Symbols and Powerups")] 
        public int maxSymbols = 10;
        public int symbolCount;
        public List<SlotSymbol> symbols = new();
        private SlotSymbolArchetypeList _symbolArchetypeList;
        public CurrentSymbol currentSymbol;

        [Header("Spinning and Results")] 
        public float baseRotationSpeed = 270.0f;
        public float currentRotationSpeed = 270.0f;
        public float firstHitWeight = 80.0f;
        public float maxHitWeight = 100.0f;
        public float mercyRule = 0.25f;
        public Spinner _spinner;
        public static bool outcomeFound;

        [Header("Shapecast Debug")]
        public float shapecastSphereRadius = 0.15f;
        public float shapecastDistance = 0.5f;

        public AudioSource wheelAudio;
        private bool _devControls;
        

        /***********************************************************************************************************************
         *** UNITY IMPLEMENTATIONS
         ***********************************************************************************************************************/
        private void Start()
        {
            outcomeFound = false;
            wheelAudio = GetComponent<AudioSource>();

            SlotSymbolArchetypeList slotSymbolOptions = GetComponentInParent<SlotSymbolArchetypeList>();
            if (!slotSymbolOptions && !isDummy)
            {
                throw new Exception("SlotWheel::Start - SlotSymbolArchetypeList not found");
            }

            _symbolArchetypeList = slotSymbolOptions;

            WheelFinder wheelFinder = GetComponentInChildren<WheelFinder>();
            if (!wheelFinder)
            {
                throw new Exception("SlotWheel::Start - WheelFinder not found");
            }

            _spinner.wheel = wheelFinder;

            ShapecasterFinder shapecasterFinder = GetComponentInChildren<ShapecasterFinder>();
            if (!shapecasterFinder)
            {
                throw new Exception("SlotWheel::Start - ShapecasterFinder not found");
            }

            _spinner.shapecaster = shapecasterFinder;

            ArrowFinder arrowFinder = GetComponentInChildren<ArrowFinder>();
            if (!arrowFinder)
            {
                throw new Exception("SlotWheel::Start - ArrowFinder not found");
            }

            _spinner.arrow = arrowFinder;

            UpdateShapecastParameters();

            _spinner.resultProbabilities = new List<double>();

            foreach (GameObject slotSymbolObject in _symbolArchetypeList.m_SlotSymbolArchetypes)
            {
                SlotSymbol slotSymbol = slotSymbolObject.GetComponent<SlotSymbol>();
                if (slotSymbol == null)
                {
                    throw new Exception("SlotSymbol not found");
                }

                if (slotSymbol.archetypeData.isDefaultSymbol)
                {
                    AddSymbol(slotSymbol);
                }
            }


            currentSymbol.index = -1;
        }

        private void FixedUpdate()
        {
            if (isDummy)
            {
                return;
            }

            if (_spinner.waitingForFirstCastHit || _spinner.waitingForFinalCastHit)
            {
                GameObject castResult = CastToGetSymbol();

                SlotSymbol symbolHit = null;
                if (castResult)
                {
                    symbolHit = castResult.GetComponent<SlotSymbol>();

                    //Send symbol to user interface (or game manager to be sent to UI)  /////////////////
                }

                // We don't want to consider the same symbol that was hit until another one is
                // unless it's the only symbol.
                if (symbolHit && (symbolHit != _spinner.lastCastHit || symbolCount == 1))
                {
                    _spinner.lastCastHit = symbolHit;
                    if (_spinner.waitingForFirstCastHit)
                    {
                        GenerateProbabilities(symbolHit);

                        // Call this on this update just to check if we should immediately stop, otherwise keep spinning.
                        DetermineSlotOutcome(symbolHit);
                    }
                    else if (_spinner.waitingForFinalCastHit)
                    {
                        if (!DetermineSlotOutcome(symbolHit))
                        {
                            _spinner.guaranteeNextHit += mercyRule;
                        }
                    }
                }
            }

            if (_spinner is { waitingForFinalCastHit: false, waitingForFirstCastHit: false, isSpinning: false } && currentSymbol.script)
            {
                Vector3 symbolFacing = currentSymbol.script.transform.forward;

                _spinner.wheel.transform.Rotate(Vector3.right, Vector3.Angle(symbolFacing, -_spinner.shapecastDirection));
            }

            if (_spinner.isSpinning)
            {
                _spinner.wheel.transform.Rotate(Vector3.right, currentRotationSpeed * Time.deltaTime);
            }
        }

        private void Update()
        {
            if (isDummy)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                _devControls = !_devControls;
            }

            if (_devControls)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    int indexToAdd = Random.Range(0, _symbolArchetypeList.m_SlotSymbolArchetypes.Count);
                    AddSymbol(_symbolArchetypeList.m_SlotSymbolArchetypes[indexToAdd].GetComponent<SlotSymbol>());
                }

                if (Input.GetKeyDown(KeyCode.X))
                {
                    RemoveSymbol(symbolCount - 1);
                }
            }
        }
        /***********************************************************************************************************************
         *** PUBLIC METHODS
         ***********************************************************************************************************************/

        /// <summary>
        ///     Quick check to see if the wheel is spinning
        /// </summary>
        /// <returns>True if wheel is spinning</returns>
        public bool IsSpinning()
        {
            return _spinner.isSpinning;
        }


        /// <summary>
        ///     Starts the wheel spinning
        /// </summary>
        public void StartSpinning()
        {
            Debug.Log("SlotWheel::StartSpinning - Start spinning called!");
            //outcomeFound = false;
            if (_spinner is { waitingForFinalCastHit: false, waitingForFirstCastHit: false, isSpinning: false })
            {
                _spinner.resultProbabilities.Clear();
                _spinner.isSpinning = true;
            }
        }


        /// <summary>
        ///     Begins the process of stopping the wheel from spinning by setting the flag to get the first symbol that crosses
        ///     paths with the shapecast after the player attempts to stop
        /// </summary>
        public void StopSpinning()
        {
            UpdateShapecastParameters();
            if (_spinner is { isSpinning: true, waitingForFinalCastHit: false, waitingForFirstCastHit: false })
            {
                _spinner.waitingForFirstCastHit = true;
            }
        }

        /// <summary>
        ///     Updates the shapecast position to compensate for wheel size adjustments
        /// </summary>
        public void UpdateShapecastParameters()
        {
            _spinner.shapecastDirection = _spinner.shapecaster.transform.position - _spinner.arrow.transform.position;
            _spinner.shapecastDirection.x = 0;
            _spinner.shapecastStartPos = _spinner.shapecaster.transform.position - _spinner.shapecastDirection * 2.0f;
            _spinner.shapecastDirection.Normalize();
        }

        /***********************************************************************************************************************
         *** PUBLIC METHODS: SYMBOL MODIFICATION
         ***********************************************************************************************************************/

        /// <summary>
        ///     Adds a new symbol to the wheel
        /// </summary>
        /// <param name="symbol">Symbol to add</param>
        /// <param name="index">Location on wheel to add symbol</param>
        public void AddSymbol(SlotSymbol symbol, int index = -1)
        {
            WheelFinder wheelFinder = GetComponentInChildren<WheelFinder>();
            MeshRenderer wheelMesh = wheelFinder.GetComponentInParent<MeshRenderer>();
            SlotSymbol slotSymbolInstance = Instantiate(symbol, wheelMesh.transform);

            UpdateSymbol(slotSymbolInstance.gameObject,
                symbols.Count,
                false);
        }

        /// <summary>
        ///     Simple wrapper to update a previous symbol based on ref if index is unknown
        /// </summary>
        /// <param name="newSymbol">Symbol to replace previous one</param>
        /// <param name="previousSymbol">Previous symbol to be replaced</param>
        /// <param name="replace">Whether or not the symbol should completely replace the existing symbol</param>
        /// <exception cref="ArgumentException">Thrown if previous symbol does not exist on this wheel</exception>
        public void UpdateSymbol(
            SlotSymbol newSymbol,
            SlotSymbol previousSymbol,
            bool replace
        )
        {
            int index = symbols.IndexOf(previousSymbol);

            if (index != -1)
            {
                throw new ArgumentException("Previous symbol referenced does not exist on this wheel", nameof(previousSymbol));
            }

            UpdateSymbol(newSymbol.gameObject,
                index,
                replace);
        }

        /// <summary>
        ///     Replaces the symbol at a desired index
        /// </summary>
        /// <param name="newSymbol">New symbol to use</param>
        /// <param name="symbolIndex">Index of symbol to replace</param>
        /// <param name="replace">Whether or not the symbol should completely replace the existing symbol</param>
        public void UpdateSymbol(
            GameObject newSymbol,
            int symbolIndex,
            bool replace
        )
        {
            if (!newSymbol)
            {
                Debug.LogError("SlotWheel::UpdateSymbol - Symbol to add is null");
                return;
            }

            bool error = false;

            if (symbolCount >= maxSymbols && !replace)
            {
                error = true;
                Debug.LogError("SlotWheel::UpdateSymbol - Cannot add more symbols to the wheel!");
            }

            SlotSymbol slotSymbolScript = newSymbol.GetComponent<SlotSymbol>();
            if (!slotSymbolScript)
            {
                error = true;
                Debug.LogError("SlotWheel::UpdateSymbol - Symbol is missing slot symbol script!!");
            }

            if (_spinner.isSpinning)
            {
                error = true;
                Debug.LogError("SlotWheel::UpdateSymbol - Cannot add slots to the wheel while it is spinning!");
            }

            if (symbolIndex < -1 || symbolIndex > symbolCount)
            {
                error = true;
                Debug.LogError("SlotWheel::UpdateSymbol - Index is invalid!");
            }

            if (error)
            {
                DestroyImmediate(newSymbol);
                return;
            }

            if (symbolIndex == -1)
            {
                symbols.Add(slotSymbolScript);
            }
            else
            {
                if (replace)
                {
                    RemoveSymbol(symbolIndex);
                }

                symbols.Insert(symbolIndex, slotSymbolScript);
            }

            symbolCount = symbols.Count;
            RedistributeSymbols();
        }

        /// <summary>
        ///     Simple wrapper to remove a previous symbol based on ref if index is unknown
        /// </summary>
        /// <param name="symbol">Symbol that should be removed</param>
        /// <exception cref="ArgumentException">Thrown if symbol does not exist on this wheel</exception>
        public void RemoveSymbol(SlotSymbol symbol)
        {
            int index = symbols.IndexOf(symbol);

            if (index != -1)
            {
                throw new ArgumentException("Previous symbol referenced does not exist on this wheel", nameof(symbol));
            }

            RemoveSymbol(index);
        }

        /// <summary>
        ///     Simply removes the symbol from the desired slot
        /// </summary>
        /// <param name="symbolIndex">Index of symbol to remove from the wheel</param>
        public void RemoveSymbol(int symbolIndex)
        {
            if (symbolCount == 1)
            {
                Debug.LogError("SlotWheel::RemoveSymbol - Must have at least one symbol!");
                return;
            }

            if (symbolIndex < 0 || symbolIndex >= symbolCount)
            {
                Debug.LogError("SlotWheel::RemoveSymbol - Index is invalid!");
                return;
            }

            if (symbolIndex < symbols.Count)
            {
                SlotSymbol toDestroy = symbols[symbolIndex];
                symbols.RemoveAt(symbolIndex);
                DestroyImmediate(toDestroy.gameObject);
                symbolCount = symbols.Count;
            }

            RedistributeSymbols();
        }
        
        /***********************************************************************************************************************
         *** PUBLIC METHODS: MISC
         ***********************************************************************************************************************/

        /// <summary>
        ///     Simple check to see if the current symbol is valid on the wheel
        /// </summary>
        /// <returns>True if symbol is valid</returns>
        public bool IsValid()
        {
            return currentSymbol.index != -1;
        }

        /***********************************************************************************************************************
         *** PRIVATE METHOD
         ***********************************************************************************************************************/

        /// <summary>
        /// Redistributes symbols around the wheel
        /// </summary>
        private void RedistributeSymbols()
        {
            double angleIncrement = 360.0f / symbols.Count;
            for (int i = 0; i < symbols.Count; i++)
            {
                SlotSymbol symbolOnWheel = symbols[i];

                Vector3 angles = new Vector3((float)(angleIncrement * i),
                    180.0f,
                    0.0f);
                symbolOnWheel.transform.eulerAngles = angles;

                symbolOnWheel.name = name + "_Slot_" + i + "_" + symbolOnWheel.archetypeData.name;
            }
        }

        /// <summary>
        ///     Casts to hit the closest symbol on the wheel
        /// </summary>
        /// <returns>The hit symbol, if any</returns>
        private GameObject CastToGetSymbol()
        {
            // DebugExtensions.DebugSphereCast(m_shapecastStartPos,
            //     m_shapecastDirection,
            //     shapecastDistance,
            //     Color.magenta,
            //     0.001f,
            //     3.0f,
            //     CastDrawType.Complete,
            //     PreviewCondition.Both,
            //     false);

            if (Physics.SphereCast(_spinner.shapecastStartPos,
                    shapecastSphereRadius,
                    _spinner.shapecastDirection,
                    out RaycastHit hit,
                    shapecastDistance,
                    LayerMask.GetMask("SlotSymbol")))
            {
                return hit.transform.gameObject;
            }

            return null;
        }

        /// <summary>
        ///     Determines a random slot to choose based on a normalized probability of the slots
        /// </summary>
        /// <returns>The chosen slot index</returns>
        private bool DetermineSlotOutcome(SlotSymbol hitSymbol)
        {
            int symbolIndex = -1;
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i] == hitSymbol)
                {
                    symbolIndex = i;
                    break;
                }
            }

            if (symbolIndex == -1)
            {
                Debug.Log("SlotWheel::DetermineSlotOutcome - Symbol not in symbol list");
                return false;
            }

            double randVal = Random.Range(0.0f, 1.0f);
            bool hit = false;

            if (randVal >= _spinner.resultProbabilities[symbolIndex])
            {
                if (symbolIndex == symbolCount - 1)
                {
                    hit = true;
                }
                else if (randVal < _spinner.resultProbabilities[symbolIndex + 1])
                {
                    hit = true;
                }
            }

            if (_spinner.guaranteeNextHit >= 1.0f)
            {
                hit = true;
            }

            if (hit)
            {
                // Update current symbol
                currentSymbol.index = symbolIndex;
                currentSymbol.script = symbols[symbolIndex];
                currentSymbol.type = currentSymbol.script.slotSymbolType;
                currentSymbol.archetype = currentSymbol.script.archetypeData;
                currentSymbol.image = currentSymbol.script.uiSymbolImage;
                currentSymbol.id = currentSymbol.script.symbolId;
                
                // Update spinner state
                _spinner.isSpinning = false;
                _spinner.waitingForFinalCastHit = false;
                _spinner.lastCastHit = null;
                _spinner.guaranteeNextHit = 0.0f;

                // Play VFX
                wheelAudio.PlayOneShot(Resources.Load<AudioClip>("Audio/SFX_SlotStop"));

                Debug.Log("Slot Wheel script says sprite is " + currentSymbol.image);
                outcomeFound = true;
            }
            
            return hit;
            
        }

        private void GenerateProbabilities(SlotSymbol firstSymbolHit)
        {
            _spinner.waitingForFirstCastHit = false;

            double secondaryHitWeight = (maxHitWeight - firstHitWeight) / (symbolCount - 1);
            double totalProbability = 0.0f;

            for (int i = 0; i < symbols.Count; i++)
            {
                double symbolProbability;
                if (firstSymbolHit == symbols[i])
                {
                    // TODO: Get probability modifier from slot machine inventory
                    symbolProbability = symbols[i]
                                            .GetProbability(symbolCount, 1.0f)
                                        * firstHitWeight;
                }
                else
                {
                    // TODO: Get probability modifier from slot machine inventory
                    symbolProbability = symbols[i]
                                            .GetProbability(symbolCount, 1.0f)
                                        * secondaryHitWeight;
                }

                totalProbability += symbolProbability;
                _spinner.resultProbabilities.Add(symbolProbability);
            }

            _spinner.resultProbabilities[0] /= totalProbability;
            for (int i = 1; i < _spinner.resultProbabilities.Count; i++)
            {
                _spinner.resultProbabilities[i] = _spinner.resultProbabilities[i] / totalProbability + _spinner.resultProbabilities[i - 1];
            }

            _spinner.waitingForFinalCastHit = true;
        }
    }
}