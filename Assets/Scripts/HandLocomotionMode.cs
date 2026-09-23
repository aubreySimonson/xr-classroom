using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace XRClassroom
{
    /// <summary>
    /// Makes both hands use the same locomotion style.
    ///
    /// The XR rig inherits the XRI Starter Assets setup, where the two hands are deliberately
    /// asymmetric: the left hand's <see cref="ControllerInputActionManager"/> has
    /// <c>Smooth Motion Enabled</c> on (thumbstick walking, no teleport) and the right hand's has it
    /// off (teleport, no walking). That single flag is why only the right hand can teleport.
    ///
    /// Each hand is teleport XOR smooth-move because both are driven by the same thumbstick, so
    /// "both hands do both" for locomotion really means "make both hands match". This component
    /// forces every <see cref="ControllerInputActionManager"/> on the rig to the chosen mode at
    /// startup (and via the context-menu item in the editor).
    ///
    /// UI pointing is unaffected: both hands' interactors already have UI interaction enabled. It is
    /// only suppressed on whichever hand is actively aiming a teleport arc, then restored on release.
    ///
    /// Put one of these anywhere in the scene (e.g. on a manager object).
    /// </summary>
    public sealed class HandLocomotionMode : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>Both hands aim-and-release teleport with the thumbstick. Snap turn on both.</summary>
            Teleport,

            /// <summary>Both hands walk continuously with the thumbstick. No teleport.</summary>
            SmoothMove,
        }

        [Tooltip("The locomotion style applied to BOTH hands.")]
        [SerializeField] Mode mode = Mode.Teleport;

        void OnEnable()
        {
            // Apply now, then keep re-applying briefly: the rig's input managers enable their input
            // actions in their own Start/OnEnable, and we want the last word.
            StartCoroutine(ApplyForAShortWhile());
        }

        IEnumerator ApplyForAShortWhile()
        {
            for (int i = 0; i < 10; i++)
            {
                Apply();
                yield return new WaitForSeconds(0.2f);
            }
        }

        [ContextMenu("Apply To Both Hands Now")]
        public void Apply()
        {
            bool smooth = mode == Mode.SmoothMove;

            ControllerInputActionManager[] managers = FindObjectsByType<ControllerInputActionManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (ControllerInputActionManager manager in managers)
            {
                if (manager.smoothMotionEnabled != smooth)
                    manager.smoothMotionEnabled = smooth; // setter re-runs the action enable/disable pass
            }
        }
    }
}
