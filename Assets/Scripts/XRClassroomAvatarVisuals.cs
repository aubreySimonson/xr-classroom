using XRMultiplayer;

namespace XRClassroom
{
    /// <summary>
    /// Keeps the template's color, host, and local-rendering behavior while removing its
    /// hard-coded assumption that voice must control blend shape zero.
    /// </summary>
    public sealed class XRClassroomAvatarVisuals : XRAvatarVisuals
    {
        public override void UpdateMouth()
        {
            // Voice-driven presentation is handled by components that consume AvatarVoiceSignal.
        }
    }
}
