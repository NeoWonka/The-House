using System;
using System.Collections.Generic;
using UnityEngine;
using Enum;
using Player;
using Data.Player;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Data.Player
{
    public class InventoryScript : MonoBehaviour
    {
        public PowerUpItem powerUpItem;
        public GameObject slotMachineObj;
        private SlotMachine slotMachineScript;

        public List<SlotSymbolArchetype> powerupInventory = new List<SlotSymbolArchetype>();
        public List<PowerUpItem> powerUpItemList= new List<PowerUpItem>();
        public int ownedPowerupCount;
        //public int powerupToAdd;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            slotMachineObj = GameObject.Find("SlotMachine");
            slotMachineScript = slotMachineObj.GetComponent<SlotMachine>();

            //List<SlotSymbolArchetype> powerupInventory = Enum.GetValues(typeof(SlotSymbolEnum)).Cast<SlotSymbolArchetype>().ToList();
            Debug.Log(powerupInventory);

            foreach (PowerUpItem powerup in powerupInventory)
            {
                ownedPowerupCount = 0;
            }
        }

    // Update is called once per frame
        void Update()
        {

        }

        public void addPowerUp()
        {
            powerUpItemList.Add(powerUpItem)
        }
    }
}
