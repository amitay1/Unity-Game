using UnityEngine;
using MinipollGame.Core;

namespace MinipollGame
{
    // מחלקת מפנה פשוטה להתגבר על בעיית שם קובץ
    [RequireComponent(typeof(Collider))]
    public class SimpleInteractable : InteractableObject
    {
        // מחלקה זו יורשת את כל ההתנהגות מ-InteractableObject
        // אין צורך להוסיף כלום - זו רק מחלקת מפנה
    }
}