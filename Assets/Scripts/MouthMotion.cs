using UnityEngine;

namespace XRClassroom
{
    /// <summary>Maps an AvatarVoiceSignal to a blend shape selected by name.</summary>
    [DisallowMultipleComponent]
    public sealed class MouthMotion : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer m_Renderer;
        [SerializeField, Tooltip("Blend shape name as shown on the Skinned Mesh Renderer.")]
        string m_BlendShapeName = "MouthOpen";
        [SerializeField] float m_QuietWeight = 100f;
        [SerializeField] float m_LoudWeight;

        int m_BlendShapeIndex = -1;

        void Awake()
        {
            ResolveBlendShape();
        }

        void ResolveBlendShape()
        {
            m_BlendShapeIndex = m_Renderer != null && m_Renderer.sharedMesh != null
                ? m_Renderer.sharedMesh.GetBlendShapeIndex(m_BlendShapeName)
                : -1;

            if (m_Renderer != null && m_BlendShapeIndex < 0)
                Debug.LogWarning($"Blend shape '{m_BlendShapeName}' was not found on {m_Renderer.name}.", this);
        }

        public void SetLevel(float loudness)
        {
            if (m_Renderer != null && m_BlendShapeIndex >= 0)
                m_Renderer.SetBlendShapeWeight(m_BlendShapeIndex, Mathf.Lerp(m_QuietWeight, m_LoudWeight, loudness));
        }
    }
}
