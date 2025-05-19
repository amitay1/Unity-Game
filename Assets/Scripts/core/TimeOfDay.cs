using UnityEngine;

namespace MinipollGame.Core
{
    // מבנה לתיאור זמן היום
    public struct TimeOfDay
    {
        public enum DayPhase
        {
            Dawn,
            Morning,
            Noon,
            Afternoon,
            Evening,
            Night,
            Midnight
        }

        public DayPhase CurrentPhase;
        public float NormalizedTime; // 0-1 representing progress through the day
        public int DayNumber;
    }
}