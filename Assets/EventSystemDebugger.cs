using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemDebugger : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null)
            Debug.LogError("EventSystem.current == NULL!");
        else if (!EventSystem.current.enabled)
            Debug.LogWarning("EventSystem ∫Ò»∞º∫»≠µ !");
    }
}