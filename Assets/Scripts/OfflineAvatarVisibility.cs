using UnityEngine;
using XRMultiplayer;

namespace XRClassroom
{
    /// <summary>Shows offline visuals while keeping this listener alive across connection changes.</summary>
    [DisallowMultipleComponent]
    public sealed class OfflineAvatarVisibility : MonoBehaviour
    {
        [SerializeField] Transform m_Visuals;

        void OnEnable()
        {
            XRINetworkGameManager.Connected.Subscribe(SetConnected);
            SetConnected(XRINetworkGameManager.Connected.Value);
        }

        void OnDisable()
        {
            XRINetworkGameManager.Connected.Unsubscribe(SetConnected);
        }

        void SetConnected(bool connected)
        {
            if (m_Visuals != null)
                m_Visuals.gameObject.SetActive(!connected);
        }
    }
}
