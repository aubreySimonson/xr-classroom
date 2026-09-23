using UnityEngine;

namespace XRClassroom
{
    /// <summary>
    /// Positions an avatar model from one tracked head transform. Model-specific offsets are
    /// explicit Inspector settings instead of assumptions hidden in the tracking code.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class AvatarPosePresentation : MonoBehaviour
    {
        [Header("Pose input")]
        [SerializeField, Tooltip("Tracked or network-replicated head pose.")]
        Transform m_TrackedHead;

        [Header("Avatar transforms")]
        [SerializeField, Tooltip("Avatar root that follows the tracked head position.")]
        Transform m_AvatarRoot;
        [SerializeField] Transform m_HeadVisualsRoot;
        [SerializeField] Transform m_Neck;
        [SerializeField] Transform m_Torso;

        [Header("Model alignment")]
        [SerializeField, Tooltip("Head visual offset in tracked-head local space.")]
        Vector3 m_HeadVisualOffset = new(0f, -0.3f, 0f);
        [SerializeField, Tooltip("Preserves the authored difference between the tracked pose and neck-bone rotation.")]
        bool m_PreserveNeckRotationOffset = true;

        [Header("Body rotation")]
        [SerializeField, Range(0f, 180f)] float m_BodyTurnThreshold = 25f;
        [SerializeField, Min(0f)] float m_BodyTurnSpeed = 3f;

        Quaternion m_NeckRotationOffset = Quaternion.identity;
        float m_TargetBodyYaw;

        void Reset()
        {
            m_AvatarRoot = transform;
        }

        void Start()
        {
            if (m_AvatarRoot == null)
                m_AvatarRoot = transform;

            if (m_TrackedHead == null)
            {
                Debug.LogError("Avatar Pose Presentation needs a tracked head transform.", this);
                enabled = false;
                return;
            }

            m_TargetBodyYaw = m_TrackedHead.eulerAngles.y;

            if (m_PreserveNeckRotationOffset && m_Neck != null)
                m_NeckRotationOffset = Quaternion.Inverse(m_TrackedHead.rotation) * m_Neck.rotation;
        }

        void LateUpdate()
        {
            Vector3 headPosition = m_TrackedHead.position;
            Quaternion headRotation = m_TrackedHead.rotation;
            Vector3 headScale = m_TrackedHead.localScale;

            m_AvatarRoot.position = headPosition;

            if (m_HeadVisualsRoot != null)
            {
                m_HeadVisualsRoot.position = headPosition + headRotation * m_HeadVisualOffset;
                m_HeadVisualsRoot.localScale = headScale;
            }

            if (m_Neck != null)
                m_Neck.rotation = headRotation * m_NeckRotationOffset;

            if (m_Torso == null)
                return;

            float currentYaw = m_Torso.eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, headRotation.eulerAngles.y)) >= m_BodyTurnThreshold)
                m_TargetBodyYaw = headRotation.eulerAngles.y;

            Quaternion targetRotation = Quaternion.Euler(0f, m_TargetBodyYaw, 0f);
            m_Torso.rotation = Quaternion.Slerp(m_Torso.rotation, targetRotation, Time.deltaTime * m_BodyTurnSpeed);
        }
    }
}
