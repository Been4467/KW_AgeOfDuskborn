using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var results = new List<RaycastResult>();
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Debug.LogWarning("레이캐스트 결과 없음 — 아무것도 안 맞음");
            }
            else
            {
                foreach (var r in results)
                    Debug.Log($"Hit: {r.gameObject.name} / depth: {r.depth} / module: {r.module}");
            }
        }
    }
}