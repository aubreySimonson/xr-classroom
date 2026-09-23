using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace XRClassroom
{
    /// <summary>
    /// Temporary headset-visible debugging helper for seeing what the active XR ray is hitting.
    /// </summary>
    public class RaycastHitDebugText : MonoBehaviour
    {
        public TMP_Text debugText;
        public string textToReplace = "Text";
        public XRRayInteractor[] rayInteractors;
        public NearFarInteractor[] nearFarInteractors;
        public LayerMask fallbackPhysicsLayers = ~0;

        float nextInteractorSearchTime;

        void Awake()
        {
            if (debugText == null)
                debugText = GetComponent<TMP_Text>();

            if (debugText == null)
                FindDebugText();

            FindRayInteractors();
        }

        void Update()
        {
            if (debugText == null)
                FindDebugText();

            if (debugText == null)
                return;

            if (Time.time >= nextInteractorSearchTime)
                FindRayInteractors();

            debugText.text = GetCurrentHitText();
        }

        void FindRayInteractors()
        {
            rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            nearFarInteractors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            nextInteractorSearchTime = Time.time + 1f;
        }

        void FindDebugText()
        {
            TMP_Text[] textObjects = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (TMP_Text textObject in textObjects)
            {
                if (textObject.text.Trim().Equals(textToReplace, System.StringComparison.OrdinalIgnoreCase))
                {
                    debugText = textObject;
                    return;
                }
            }

            foreach (TMP_Text textObject in textObjects)
            {
                if (textObject.name.ToLower().Contains("debug") || textObject.text.ToLower().Contains("debug"))
                {
                    debugText = textObject;
                    return;
                }
            }
        }

        string GetCurrentHitText()
        {
            foreach (XRRayInteractor rayInteractor in rayInteractors)
            {
                if (rayInteractor == null || !rayInteractor.isActiveAndEnabled)
                    continue;

                if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit) && hit.collider != null)
                    return FormatHit("XR ray physics hit", rayInteractor.name, hit.collider.gameObject, hit.point);
            }

            foreach (NearFarInteractor nearFarInteractor in nearFarInteractors)
            {
                if (nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
                    continue;

                if (TryGetNearFarPhysicsHit(nearFarInteractor, out RaycastHit hit))
                    return FormatHit("Near/Far physics hit", nearFarInteractor.name, hit.collider.gameObject, hit.point);

                if (nearFarInteractor.hasHover && nearFarInteractor.interactablesHovered.Count > 0)
                {
                    Transform hoveredTransform = nearFarInteractor.interactablesHovered[0].transform;
                    return FormatHit("Near/Far hover object", nearFarInteractor.name, hoveredTransform.gameObject, hoveredTransform.position);
                }
            }

            return "Ray hit: nothing";
        }

        bool TryGetNearFarPhysicsHit(NearFarInteractor nearFarInteractor, out RaycastHit hit)
        {
            hit = default;

            ICurveInteractionDataProvider curveData = nearFarInteractor as ICurveInteractionDataProvider;
            if (curveData == null || !curveData.isActive)
                return false;

            var samplePoints = curveData.samplePoints;
            for (int i = 1; i < samplePoints.Length; i++)
            {
                Vector3 start = samplePoints[i - 1];
                Vector3 end = samplePoints[i];
                Vector3 direction = end - start;
                float distance = direction.magnitude;

                if (distance <= 0f)
                    continue;

                if (Physics.Raycast(start, direction / distance, out hit, distance, fallbackPhysicsLayers, QueryTriggerInteraction.Ignore))
                    return true;
            }

            return false;
        }

        string FormatHit(string hitType, string interactorName, GameObject hitObject, Vector3 hitPoint)
        {
            return $"{hitType}: {hitObject.name}\nInteractor: {interactorName}\nPoint: {hitPoint.x:0.00}, {hitPoint.y:0.00}, {hitPoint.z:0.00}";
        }
    }
}
