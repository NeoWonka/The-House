using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using Enum;

namespace Data.Player
{
    [CreateAssetMenu(fileName = "PowerUp", menuName = "PowerUps/PowerUpItem")]
    public class PowerUpItem : ScriptableObject
    {

        public struct MachineModifiers
        {
        }

        public struct WheelModifiers
        {
            public float spinSpeedModifier;
        }

        public struct SymbolModifiers
        {
            public SlotSymbolEnum slotSymbolToModify;
            public float patronMoneyModifier;
            public float patronStressModifier;
            public float comboMultiplierModifier;
            public float probabilityModifier;
        }
        
        public struct PatronModifiers
        {
            public float patronStressScalar;
            public float patronMoneyScalar;
        }
        
        public GameObject itemSprite;
        public string powerUpType;

        public MachineModifiers machineModifiers;
        public WheelModifiers wheelModifiers;
        public List<SymbolModifiers> symbolModifiers;
        public PatronModifiers patronModifiers; 
    }
}
