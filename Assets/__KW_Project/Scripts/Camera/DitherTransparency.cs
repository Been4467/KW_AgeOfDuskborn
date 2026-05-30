using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace KW
{
    public class DitherTransparency : MonoBehaviour
    {
        [SerializeField] public Transform player;
        [SerializeField] private LayerMask occluderLayer;
        [SerializeField, Range(0f, 1f)] private float transparentAlpha = 0.2f;
        [SerializeField] private float fadeSpeed = 5f;

        private Camera mainCamera;
        private Dictionary<Renderer, float> managedRenderers = new Dictionary<Renderer, float>();

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            // if (player == null) Debug.LogError("[DitherTransparency] 플레이어가 없음");

            if (player == null || mainCamera == null) return;

            Vector3 direction = (player.position - mainCamera.transform.position).normalized;
            float distance = Vector3.Distance(mainCamera.transform.position, player.position);
            RaycastHit[] hits = Physics.RaycastAll(mainCamera.transform.position, direction, distance, occluderLayer);

            HashSet<Renderer> currentOccluders = new HashSet<Renderer>();
            foreach (var hit in hits)
            {
                // 콜라이더가 맞은 오브젝트의 '자식'들에 있는 모든 렌더러를 찾습니다.
                Renderer[] renderers = hit.collider.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        currentOccluders.Add(rend);
                    }
                }
            }

            foreach (var rend in currentOccluders)
            {
                if (!managedRenderers.ContainsKey(rend))
                {
                    managedRenderers.Add(rend, 1f);
                }
            }

            List<Renderer> toRemove = new List<Renderer>();
            List<Renderer> rendererKeys = new List<Renderer>(managedRenderers.Keys);

            foreach (var rend in rendererKeys)
            {
                if (rend == null) // 오브젝트가 파괴된 경우를 대비
                {
                    toRemove.Add(rend);
                    continue;
                }

                float currentAlpha = managedRenderers[rend];
                float targetAlpha = currentOccluders.Contains(rend) ? transparentAlpha : 1f;
                float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

                Material[] materials = rend.materials;
                foreach (Material mat in materials)
                {
                    if (mat != null && mat.HasProperty("_BaseColor"))
                    {
                        Color color = mat.GetColor("_BaseColor");
                        color.a = newAlpha;
                        mat.SetColor("_BaseColor", color);
                    }
                }

                managedRenderers[rend] = newAlpha;

                if (Mathf.Approximately(newAlpha, 1f) && !currentOccluders.Contains(rend))
                {
                    toRemove.Add(rend);
                }
            }

            foreach (var rend in toRemove)
            {
                if (rend != null)
                    managedRenderers.Remove(rend);
            }
        }
    }
}