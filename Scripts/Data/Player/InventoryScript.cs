/*using System;
using System.Collections.Generic;
using UnityEngine;
using Enum;
using Player;
using Data.Player;
using System.Linq;
using System.Diagnostics;

public class InventoryScript : MonoBehaviour
{
    public GameObject SlotMachineObj;
    private SlotMachine slotMachineScript;

    public List<SlotSymbolArchetype> powerupInventory = new List<SlotSymbolArchetype>();
    public int ownedPowerupCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SlotMachineObj = GameObject.Find("SlotMachine");
        SlotMachine slotMachineScript = SlotMachineObj.GetComponent<SlotMachine>();

        List<SlotSymbolArchetype> powerupInventory = Enum.GetValues(typeof(SlotSymbolEnum)).Cast<SlotSymbolArchetype>().ToList();
        Debug.Log(powerupInventory);

        foreach (SlotSymbolArchetype powerup in powerupInventory)
        {
            ownedPowerupCount = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
         
    }

    void addPowerUp()
    {
        int powerupToAdd;
        slotMachineScript.GetSlotsResult() = powerupToAdd;
    }
} */
