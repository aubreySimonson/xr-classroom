using Unity.XR.CoreUtils;
using UnityEngine;

namespace XRClassroom
{
    /// <summary>Copies the local XR camera pose into a clearly identified avatar pose target.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class LocalHeadPoseSource : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional explicit XR Origin. The local scene origin is discovered if left empty.")]
        XROrigin m_XROrigin;
        [SerializeField, Tooltip("Transform that receives the XR camera's world pose.")]
        Transform m_Output;

        Transform m_Camera;

        void Start()
        {
            if (m_XROrigin == null)
                m_XROrigin = FindFirstObjectByType<XROrigin>();

            m_Camera = m_XROrigin == null || m_XROrigin.Camera == null
                ? null
                : m_XROrigin.Camera.transform;

            if (m_Camera == null || m_Output == null)
            {
                Debug.LogError("Local Head Pose Source needs an XR camera and an output transform.", this);
                enabled = false;
            }
        }

        void LateUpdate()
        {
            m_Output.SetPositionAndRotation(m_Camera.position, m_Camera.rotation);
            m_Output.localScale = m_Camera.localScale;
        }
    }
}
