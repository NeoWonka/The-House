/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using Data.Player;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Component = UnityEngine.Component;
using Image = UnityEngine.UI.Image;
using Debug = UnityEngine.Debug;


namespace Player
{
    public class SlotResultsUI : MonoBehaviour
    {
        [Header("Slot Machine")]
        public GameObject slotMachine;
        public SlotMachine slotMachineScript;
        //public GameObject slotWheel;
        public SlotWheel slotWheelScript;
        public int currentWheelSpinning;
        public List<GameObject> slotWheels = new List<GameObject>();
        public List<SlotWheel> slotWheelScripts = new List<SlotWheel>();
        


        [Header("UI")]
        public GameObject resultUIContainer;
        public List<GameObject> resultUIContainerList = new List<GameObject>();
        public GameObject slotSymbolUIImgSlot;
        public Image slotSymbolUIImage;
        public GameObject slotSymbolUI;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ///GameObjects
            currentWheelSpinning = slotMachineScript.currentWheel;
            slotMachine = GameObject.FindWithTag("Player");
            slotMachineScript = slotMachine.GetComponent<SlotMachine>();
            //slotWheelScript = slotWheel.GetComponent<SlotWheel>();
            List<SlotWheel> slotWheelScripts = new List<SlotWheel>();

            ///UI
            slotSymbolUI = GameObject.FindWithTag("ResultUI_Container");


            ///Create lists, and instantiate UI
            foreach (SlotWheel slotwheelscript in slotMachineScript.slotWheelScripts)
            {
                slotWheelScripts.Add(slotwheelscript);
            }

            foreach (SlotWheel slotWheelScript in slotWheelScripts)
            {
                GameObject newContainer = Instantiate(resultUIContainer, slotSymbolUI.transform);
                resultUIContainerList.Add(newContainer);
            }
        }

        // Update is called once per frame
        void Update()
        {
            foreach (SlotWheel slotWheelScript in slotWheelScripts)
            {
                //bool outcomeFound = slotWheelScript.outcomeFound;
                /*if (SlotWheel.outcomeFound = true)
                {
                    Debug.Log(slotWheelScript.currentSymbol.image);
                    resultUIContainerList[currentWheelSpinning].transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
                           slotWheelScripts[currentWheelSpinning].GetComponent<SlotWheel>().currentSymbol.image;

                    //currentWheel++;
                }

                Debug.Log(slotWheelScript.currentSymbol.image);
                resultUIContainerList[currentWheelSpinning].transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
                       slotWheelScripts[currentWheelSpinning].GetComponent<SlotWheel>().currentSymbol.image;
            }
        }

    }
}*/
