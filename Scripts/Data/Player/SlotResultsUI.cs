/*using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System;
using Data.Player;
using UnityEngine;
using Component = UnityEngine.Component;
using UnityEngine.UI;

public class SlotResultsUI : MonoBehaviour
{
    public GameObject SlotMachine;

    public List<GameObject> slotWheels = new List<GameObject>();
    public List<SlotWheel> slotWheelScripts = new List<slotWheels>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SlotMachine = GameObject.FindWithTag("Player");

        slotWheels = SlotMachine.slotWheels;
        foreach (GameObject wheel in slotWheels)
        {
            SlotWheel wheelScript = wheel.GetCompenent<slotWheel>();
        }

        slotSymbolUI = GameObject.FindWithTag("ResultUI_Container");

        
        slotMachineScript = SlotMachine.GetComponent<SlotMachine>();

    }

    // Update is called once per frame
    void Update()
    {
        foreach(GameObject slotWheelScript in slotWheelScripts)
        {
            Debug.Log(slotWheelScript.currentSymbol.image);
        }
    }

    /*slotSymbolUIImage = slotSymbolUI.transform.GetComponentsInChildren;
    slotSymbolUIImage = GameObject.FindWithTag("ResultUI_ImgSlot").GetComponent<Image>().sprite;
    slotSymbolUIImage = currentSymbol.image;
    
} */

