using System;

namespace Data.Player
{
    [Serializable]
    public class SlotsResult
    {
        public long m_patronMoneyAdjustment;
        public double m_patronStressAdjustment;
        public bool m_isJackpot;
        public bool m_isCombo;
        public DateTime m_finalWheelLockTime;

        public override string ToString()
        {
            return "Result:\n\t" +
                   $"Money: {m_patronMoneyAdjustment}\n\t" +
                   $"Stress: {m_patronStressAdjustment}\n\t" +
                   $"Jackpot: {m_isJackpot}\n\t" +
                   $"Combo: {m_isCombo}\n\t" +
                   $"Final Wheel Lock Time: {m_finalWheelLockTime}";
        }
    }
}