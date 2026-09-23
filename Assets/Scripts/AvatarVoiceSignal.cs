using System;
using UnityEngine;
using UnityEngine.Events;
using XRMultiplayer;

namespace XRClassroom
{
    /// <summary>
    /// Turns the voice level received by XRINetworkPlayer into a reusable avatar signal.
    /// This component deliberately does not know how the avatar will respond to that signal.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRINetworkPlayer))]
    public sealed class AvatarVoiceSignal : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField, Tooltip("The network player that receives this avatar's Vivox voice level.")]
        XRINetworkPlayer m_NetworkPlayer;

        [Header("Runtime values (visible while playing)")]
        [SerializeField, Range(0f, 1f), Tooltip("The current voice level supplied by XRINetworkPlayer after its Vivox smoothing.")]
        float m_RawLoudness;

        [SerializeField, Range(0f, 1f), Tooltip("The reusable loudness value after applying the remapping curve.")]
        float m_Loudness;

        [Header("Remapping")]
        [SerializeField, Tooltip("Maps incoming voice level (horizontal) to output (vertical).")]
        AnimationCurve m_Remap = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Keeps remapped output between zero and one.")]
        bool m_ClampOutput = true;

        [Header("Output")]
        [SerializeField, Tooltip("Connect loudness to any public method that accepts one float.")]
        UnityEvent<float> m_OnLoudnessChanged = new();

        public float RawLoudness => m_RawLoudness;
        public float Loudness => m_Loudness;
        public UnityEvent<float> OnLoudnessChanged => m_OnLoudnessChanged;
        public event Action<float> LoudnessChanged;

        void Reset()
        {
            m_NetworkPlayer = GetComponent<XRINetworkPlayer>();
        }

        void Awake()
        {
            if (m_NetworkPlayer == null)
                m_NetworkPlayer = GetComponent<XRINetworkPlayer>();
        }

        void Update()
        {
            m_RawLoudness = m_NetworkPlayer == null
                ? 0f
                : Mathf.Clamp01(m_NetworkPlayer.playerVoiceAmp);

            m_Loudness = m_Remap == null
                ? m_RawLoudness
                : m_Remap.Evaluate(m_RawLoudness);

            if (m_ClampOutput)
                m_Loudness = Mathf.Clamp01(m_Loudness);

            LoudnessChanged?.Invoke(m_Loudness);
            m_OnLoudnessChanged.Invoke(m_Loudness);
        }
    }
}
