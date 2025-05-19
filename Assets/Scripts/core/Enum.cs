namespace MinipollGame.Core
{
    // סוגי רגשות בסיסיים
    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Scared,
        Curious,
        Tired,
        Hungry,
        Excited,
        Shy,
        Angry,
        Surprised
    }

    // מצבי תנועה של מיניפול
    public enum MinipollMovementState
    {
        Idle,
        Walking,
        Running,
        Sleeping,
        Dancing,
        Hiding
    }

    // סוגי החלטות שמיניפול יכול לקבל
    public enum DecisionType
    {
        Movement,
        Interaction,
        SocialBehavior,
        Rest,
        Explore
    }

    // מצבי חיים בסיסיים
    public enum MinipollLifeState
    {
        Awake,
        Asleep,
        Hibernating
    }

    // סוגי יחסים בין מיניפולים
    public enum RelationshipType
    {
        Stranger,
        Acquaintance,
        Friend,
        BestFriend,
        Afraid,
        Hostile
    }
}