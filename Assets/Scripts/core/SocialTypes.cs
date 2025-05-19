// === GPT ADD START: Social & Need definitions ===
namespace MinipollGame.Core
{
    /// <summary>Basic physiological or psychological needs a Minipoll tracks (0‑1 scale).</summary>
    public enum NeedType
    {
        Energy,
        Hunger,
        Social,
        Fun,
        Hygiene
    }

    /// <summary>Definition used by MinipollNeedsSystem for custom editor tweaking.</summary>
    [System.Serializable]
    public struct NeedDefinition
    {
        public NeedType type;
        public float initialValue;   // 0‑1
        public float decayRate;      // units per second
    }


    /// <summary>High‑level social actions Minipolls can perform.</summary>
    public enum SocialInteractionType
    {
        Greeting,
        Play,
        ShareExperience,
        SeekComfort,
        Comfort,
        Observe,
        Reject,
        Ignore
    }

    /// <summary>Runtime relationship data held per partner Minipoll.</summary>
    [System.Serializable]
    public struct RelationshipData
    {
        public MinipollBrain otherMinipoll;
        public float trust;         // 0‑1
        public float friendship;    // 0‑1
        public float fear;          // 0‑1
        public RelationshipType relationshipType;
        public float lastInteractionTime;
        public System.Collections.Generic.List<string> sharedExperiences;

        /// <summary>Weighted average describing overall “positivity” of the relationship.</summary>
        public float GetOverallScore()
        {
            return (trust * 0.5f) + (friendship * 0.5f) - (fear * 0.4f);
        }
    }
}
// === GPT ADD END ===
