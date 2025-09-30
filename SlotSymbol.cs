using System;
using Data.Player;
using Enum;
using UnityEngine;

namespace Player
{
    public class SlotSymbol : MonoBehaviour
    {
        /***********************************************************************************************************************
         *** CLASS MEMBERS
         ***********************************************************************************************************************/
        public SlotSymbolArchetype archetypeData;
        public SlotSymbolEnum slotSymbolType;
        public Guid symbolId;
        public Sprite uiSymbolImage;

        /***********************************************************************************************************************
         *** UNITY IMPLEMENTATIONS
         ***********************************************************************************************************************/
        private void Start()
        {
            if (archetypeData == null)
            {
                throw new NullReferenceException("SlotSymbol script is null");
            }

            symbolId = Guid.NewGuid();
            slotSymbolType = archetypeData.slotSymbolType;

            uiSymbolImage = archetypeData.ui.image;

            Renderer symbolRenderer = GetComponent<Renderer>();

            // Use SetColor to set the main color shader property
            symbolRenderer.material.SetColor("_BaseColor", archetypeData.ui.color);
        }

        /***********************************************************************************************************************
         *** OVERLOADS
         ***********************************************************************************************************************/
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(SlotSymbol))
            {
                return false;
            }

            return symbolId == ((SlotSymbol)obj).symbolId;
        }

        public override int GetHashCode()
        {
            return symbolId.GetHashCode();
        }
        
        /***********************************************************************************************************************
         *** PUBLIC METHODS: SYMBOL RESULTS
         ***********************************************************************************************************************/
        /// <summary>
        ///     Gets the total unweighted probability of landing on the slot
        /// </summary>
        public double GetProbability(int slotCount, float probabilityModifier)
        {
            return 1.0d / slotCount + archetypeData.powerup.probabilityAdd * probabilityModifier;
        }

        /// <summary>
        ///     Gets the total money adjustment for the patron based on the slot symbol type, level, powerups, and multipliers
        /// </summary>
        /// <param name="isCombo">Whether the patron rolled a combo</param>
        public long GetPatronMoneyAdjustment(bool isCombo, float moneyModifier, float comboModifier)
        {
            return (long)((archetypeData.basePatron.moneyScalar
                           + archetypeData.powerup.moneyScalar * moneyModifier)
                          * GetMultiplier(isCombo, comboModifier));
        }

        /// <summary>
        ///     Gets the total stress adjustment for the patron based on the slot symbol type, level, powerups, and multipliers
        /// </summary>
        /// <param name="isCombo">Whether the patron rolled a combo</param>
        public double GetPatronStressAdjustment(bool isCombo, float stressModifier, float comboModifier)
        {
            return (archetypeData.basePatron.stressScalar
                    + archetypeData.powerup.stressScalar * stressModifier)
                   * GetMultiplier(isCombo, comboModifier);
        }

        /***********************************************************************************************************************
         *** PRIVATE METHODS
         ***********************************************************************************************************************/

        /// <summary>
        ///     Gets the multiplier to be used for calculating the stress and money adjustments for the patron
        /// </summary>
        /// <param name="isCombo">Whether the patron rolled a combo</param>
        private float GetMultiplier(bool isCombo, float comboModifier)
        {
            float multiplier = 1.0f;

            if (isCombo)
            {
                multiplier *= archetypeData.basePatron.comboMultiplier +
                              archetypeData.powerup.comboAdd * comboModifier;
            }

            return multiplier;  
        } 
    }
}