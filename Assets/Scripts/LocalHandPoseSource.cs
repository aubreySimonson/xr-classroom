using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace XRClassroom
{
    /// <summary>
    /// Finds the local XR rig's current hand/controller transforms and exposes the active pair.
    /// This is an input-source adapter, not an avatar visual script.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class LocalHandPoseSource : MonoBehaviour
    {
        public XRInputModalityManager inputModalityManager;

        [Header("Controller sources")]
        public Transform leftControllerPosition;
        public Transform rightControllerPosition;

        [Header("Hand tracking sources")]
        public Transform leftTrackedHandPosition;
        public Transform rightTrackedHandPosition;

        [Header("Active output")]
        public XRInputModalityManager.InputMode currentInputMode;
        public Transform activeLeftHandPosition;
        public Transform activeRightHandPosition;

        void OnEnable()
        {
            ResolveSources();
            ChooseActiveSources();
        }

        void LateUpdate()
        {
            ResolveSources();
            ChooseActiveSources();
        }

        void ResolveSources()
        {
            if (inputModalityManager == null)
                inputModalityManager = GetComponent<XRInputModalityManager>();

            if (inputModalityManager == null)
                inputModalityManager = FindFirstObjectByType<XRInputModalityManager>();

            if (inputModalityManager != null)
            {
                if (leftControllerPosition == null && inputModalityManager.leftController != null)
                    leftControllerPosition = inputModalityManager.leftController.transform;

                if (rightControllerPosition == null && inputModalityManager.rightController != null)
                    rightControllerPosition = inputModalityManager.rightController.transform;

                if (leftTrackedHandPosition == null && inputModalityManager.leftHand != null)
                {
                    XRHandSkeletonDriver leftHandSkeleton = inputModalityManager.leftHand.GetComponentInChildren<XRHandSkeletonDriver>();
                    if (leftHandSkeleton != null)
                        leftTrackedHandPosition = leftHandSkeleton.rootTransform;
                }

                if (rightTrackedHandPosition == null && inputModalityManager.rightHand != null)
                {
                    XRHandSkeletonDriver rightHandSkeleton = inputModalityManager.rightHand.GetComponentInChildren<XRHandSkeletonDriver>();
                    if (rightHandSkeleton != null)
                        rightTrackedHandPosition = rightHandSkeleton.rootTransform;
                }
            }
        }

        void ChooseActiveSources()
        {
            currentInputMode = XRInputModalityManager.currentInputMode.Value;

            if (currentInputMode == XRInputModalityManager.InputMode.TrackedHand)
            {
                activeLeftHandPosition = leftTrackedHandPosition;
                activeRightHandPosition = rightTrackedHandPosition;
            }
            else
            {
                activeLeftHandPosition = leftControllerPosition;
                activeRightHandPosition = rightControllerPosition;
            }
        }
    }
}
