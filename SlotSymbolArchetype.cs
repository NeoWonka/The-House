using Enum;
using UnityEngine;
//using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
namespace Data.Player
{
    [CreateAssetMenu(fileName = "SlotSymbolArchetype", menuName = "Scriptable Objects/SlotSymbolArchetype")]
    public class SlotSymbolArchetype : ScriptableObject
    {
        [Serializable]
        public struct BasePatron
        {
            public long moneyScalar;
            public long stressScalar;
            public float comboMultiplier;
        }

        [Serializable]
        public struct Powerup
        {
            public long moneyScalar;
            public long stressScalar;
            public float comboAdd;
            public double probabilityAdd;
        }

        [Serializable]
        public struct UI
        {
            public Color color;
            public Sprite image;
        }

        //[FormerlySerializedAs("slotSymbol")]
        public SlotSymbolEnum slotSymbolType;
        public float levelMultiplier;
        public bool isDefaultSymbol;

        public BasePatron basePatron;
        public Powerup powerup;
        public UI ui;
    }
}