using DevionGames.StatSystem;
using DevionGames;            // for EventHandler
using UnityEngine;

namespace DevionGames.StatSystem
{
    [System.Serializable]
    public class Level : Stat
    {
        [StatPicker]
        [SerializeField] protected Attribute m_Experience;

        public override void Initialize(StatsHandler handler, StatOverride statOverride)
        {
            base.Initialize(handler, statOverride);

            // 1) Grab the “Experience” Attribute
            this.m_Experience = handler.GetStat(this.m_Experience.Name) as Attribute;

            // 2) Remember last known EXP so we can show deltas
            float lastKnownExp = this.m_Experience.CurrentValue;

            // (A) Existing “level up” listener
            this.m_Experience.onCurrentValueChange += () =>
            {
                if (this.m_Experience.CurrentValue >= this.m_Experience.Value)
                {
                    this.m_Experience.CurrentValue = 0f;
                    Add(1f);  // fires the built-in “+1 Level” notification
                }
            };

            // (B) NEW: Fire an “OnExperienceGained” event whenever EXP increases
            this.m_Experience.onCurrentValueChange += () =>
            {
                float newExp = this.m_Experience.CurrentValue;
                float delta = newExp - lastKnownExp;
                if (delta > 0f)
                {
                    // Broadcast a Devion “OnExperienceGained” event, passing the integer delta
                    EventHandler.Execute("OnExperienceGained", Mathf.RoundToInt(delta));
                }
                lastKnownExp = newExp;
            };
        }
    }
}
