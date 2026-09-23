using Unity.Netcode;
using UnityEngine;

namespace XRClassroom
{
    /// <summary>
    /// Hides selected avatar objects and renderers only for the player who owns this network avatar.
    /// Remote players still see the full avatar.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalAvatarHiddenRenderers : NetworkBehaviour
    {
        [Header("GameObjects hidden from the owning player")]
        [SerializeField, Tooltip("Use this for whole face-part objects that clip into the local VR camera. Remote players still see these objects.")]
        GameObject[] m_GameObjectsToHideFromOwner;

        [Header("Renderers hidden from the owning player")]
        [SerializeField, Tooltip("Use this for individual renderers when you do not want to hide the whole GameObject. Remote players still see these renderers.")]
        Renderer[] m_RenderersToHideFromOwner;

        bool[] m_OriginalGameObjectActiveStates;
        bool[] m_OriginalRendererEnabledStates;

        void Awake()
        {
            CacheOriginalStates();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetHidden(IsOwner);
        }

        public override void OnNetworkDespawn()
        {
            RestoreOriginalStates();
            base.OnNetworkDespawn();
        }

        void OnDestroy()
        {
            RestoreOriginalStates();
        }

        void CacheOriginalStates()
        {
            if (m_GameObjectsToHideFromOwner != null)
            {
                m_OriginalGameObjectActiveStates = new bool[m_GameObjectsToHideFromOwner.Length];
                for (int i = 0; i < m_GameObjectsToHideFromOwner.Length; i++)
                {
                    GameObject objectToHide = m_GameObjectsToHideFromOwner[i];
                    m_OriginalGameObjectActiveStates[i] = objectToHide != null && objectToHide.activeSelf;
                }
            }

            if (m_RenderersToHideFromOwner != null)
            {
                m_OriginalRendererEnabledStates = new bool[m_RenderersToHideFromOwner.Length];
                for (int i = 0; i < m_RenderersToHideFromOwner.Length; i++)
                {
                    Renderer rendererToHide = m_RenderersToHideFromOwner[i];
                    m_OriginalRendererEnabledStates[i] = rendererToHide != null && rendererToHide.enabled;
                }
            }
        }

        void SetHidden(bool hidden)
        {
            if (m_GameObjectsToHideFromOwner != null)
            {
                for (int i = 0; i < m_GameObjectsToHideFromOwner.Length; i++)
                {
                    GameObject objectToHide = m_GameObjectsToHideFromOwner[i];
                    if (objectToHide == null)
                        continue;

                    bool originalState = m_OriginalGameObjectActiveStates != null && i < m_OriginalGameObjectActiveStates.Length
                        ? m_OriginalGameObjectActiveStates[i]
                        : objectToHide.activeSelf;

                    objectToHide.SetActive(hidden ? false : originalState);
                }
            }

            if (m_RenderersToHideFromOwner != null)
            {
                for (int i = 0; i < m_RenderersToHideFromOwner.Length; i++)
                {
                    Renderer rendererToHide = m_RenderersToHideFromOwner[i];
                    if (rendererToHide == null)
                        continue;

                    bool originalState = m_OriginalRendererEnabledStates != null && i < m_OriginalRendererEnabledStates.Length
                        ? m_OriginalRendererEnabledStates[i]
                        : rendererToHide.enabled;

                    rendererToHide.enabled = hidden ? false : originalState;
                }
            }
        }

        void RestoreOriginalStates()
        {
            if (m_GameObjectsToHideFromOwner != null && m_OriginalGameObjectActiveStates != null)
            {
                for (int i = 0; i < m_GameObjectsToHideFromOwner.Length && i < m_OriginalGameObjectActiveStates.Length; i++)
                {
                    GameObject objectToHide = m_GameObjectsToHideFromOwner[i];
                    if (objectToHide != null)
                        objectToHide.SetActive(m_OriginalGameObjectActiveStates[i]);
                }
            }

            if (m_RenderersToHideFromOwner != null && m_OriginalRendererEnabledStates != null)
            {
                for (int i = 0; i < m_RenderersToHideFromOwner.Length && i < m_OriginalRendererEnabledStates.Length; i++)
                {
                    Renderer rendererToHide = m_RenderersToHideFromOwner[i];
                    if (rendererToHide != null)
                        rendererToHide.enabled = m_OriginalRendererEnabledStates[i];
                }
            }
        }
    }
}
