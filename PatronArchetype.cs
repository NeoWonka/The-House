using System;
using Enum;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Data.NPC
{
    [CreateAssetMenu(fileName = "PatronArchetype", menuName = "Scriptable Objects/PatronArchetype")]
    public class PatronArchetype : ScriptableObject
    {
        /*******************
         * Randomization
         *******************/
        public PatronArchetypeEnum archetype;
        public string patronName;

        public float minMinMoneyThreshold;
        public float maxMinMoneyThreshold;

        public int minStartingMoney;
        public int maxStartingMoney;

        public float minWinningsThresholdMult;
        public float maxWinningsThresholdMult;

        public float minLossThresholdMult;
        public float maxLossThresholdMult;

        public int minStartingStress;
        public int maxStartingStress;

        public int minStressQuitLimit;
        public int maxStressQuitLimit;
        public int maxStressLeaveLimit;

        public float minStressMultiplier;
        public float maxStressMultiplier;

        public float minTimeToSpinSeconds;
        public float maxTimeToSpinSeconds;

        public double stressTimeMultiplier;

        public int GetStartingMoney()
        {
            return Random.Range(minStartingMoney, maxStartingMoney);
        }

        public float GetWinningsThresholdMult()
        {
            return Random.Range(minWinningsThresholdMult, maxWinningsThresholdMult);
        }

        public float GetLossThresholdMult()
        {
            return Random.Range(-minLossThresholdMult, -maxLossThresholdMult);
        }

        public float GetMinMoneyThreshold()
        {
            return Random.Range(minMinMoneyThreshold, maxMinMoneyThreshold);
        }

        public int GetStartingStress()
        {
            return Random.Range(minStartingStress, maxStartingStress);
        }

        public int GetStressQuitLimit()
        {
            return Random.Range(minStressQuitLimit, maxStressQuitLimit);
        }

        public int GetStressLeaveLimit()
        {
            return Random.Range(maxStressQuitLimit, maxStressLeaveLimit);
        }

        public double GetStressMultiplier()
        {
            return Random.Range(minStressMultiplier, maxStressMultiplier);
        }

        public int GetTimeToSpinMillis()
        {
            return (int)(Random.Range(minTimeToSpinSeconds, maxTimeToSpinSeconds) * 1000.0f);
        }
    }
}