using UnityEngine;
using UnityEngine.XR.Templates.VRMultiplayer;
using Unity.XR.CoreUtils;
using XRMultiplayer;

namespace XRClassroom
{
    /// <summary>
    /// Written by a mix of codex and Aubrey (mostly codex), summer 2026
    /// Presents a simple part-based avatar from tracked or network-replicated head and hand poses.
    /// XR Classroom avatars are intentionally GameObjects, not armatures. They have a head, a body, maybe hands, and nothing else.
    /// </summary>
    
    //this script reads tracking/network transforms and then moves avatar visuals.
    ////Running late helps ensure the source transforms have already been updated this frame.
    [DefaultExecutionOrder(100)] 

    //This controls where the player's avatar body is, so there should not be two of them unless something has gone very wrong
    [DisallowMultipleComponent]
    public sealed class AvatarPositioner : MonoBehaviour
    {
        [Header("Pose input")]
        public bool isSelf;
        public XRINetworkPlayer networkPlayer;//Optional -- the script will find this in the scene if you don't connect it in the inspector.

        [Header("Self pose input")]
        public LocalHandPoseSource localHandPoseSource;
        public Transform selfHeadPosition;
        public Transform selfLeftHandPosition;
        public Transform selfRightHandPosition;

        [Header("Other player pose input")]
        public Transform playerHeadPosition;
        public Transform playerLeftHandPosition;
        public Transform playerRightHandPosition;


        public Transform avatarRoot;// This is for things like where your nametag should be, or for when the system needs to move the player. 

        [Header("Avatar part GameObjects")]
        public Transform headObject;
        public Transform bodyObject;
        public Transform leftHandObject;
        public Transform rightHandObject;

        [Header("Model Alignment Adjust")]
        public Vector3 headOffset;
        public Vector3 bodyOffset;
        public Vector3 leftHandOffset;
        public Vector3 rightHandOffset;
        public Vector3 headRotationAdjust;
        public Vector3 bodyRotationAdjust;
        public Vector3 leftHandRotationAdjust;
        public Vector3 rightHandRotationAdjust;

        [Header("HMD Rotation Adjust")]
        public Vector3 hmdHeadRotationAdjust;
        public Vector3 hmdBodyRotationAdjust;
        public Vector3 hmdLeftHandRotationAdjust;
        public Vector3 hmdRightHandRotationAdjust;

        [Header("Rotation")]
        public bool preserveAuthoredRotationOffsets = true;
        public bool bodyUsesHeadYawOnly = true;

        [Header("Body rotation")]
        public float bodyTurnThreshold = 25f;//how far the head has to turn before the body turns
        public float bodyTurnSpeed = 3f;//in [units]

        Quaternion headRotationOffset = Quaternion.identity;
        Quaternion bodyRotationOffset = Quaternion.identity;
        Quaternion leftHandRotationOffset = Quaternion.identity;
        Quaternion rightHandRotationOffset = Quaternion.identity;
        Transform activeHeadPosition;
        Transform activeLeftHandPosition;
        Transform activeRightHandPosition;
        float targetBodyYaw;

        void Start()
        {
            if (avatarRoot == null)
                avatarRoot = transform;

            ResolvePoseInputs();

            if (activeHeadPosition != null)
            {
                targetBodyYaw = activeHeadPosition.eulerAngles.y; 

                if (preserveAuthoredRotationOffsets)
                {
                    headRotationOffset = GetRotationOffset(activeHeadPosition, headObject);
                    bodyRotationOffset = GetRotationOffset(GetBodyRotation(), bodyObject);
                    leftHandRotationOffset = GetRotationOffset(activeLeftHandPosition, leftHandObject);
                    rightHandRotationOffset = GetRotationOffset(activeRightHandPosition, rightHandObject);
                }
            }
            else
            {
                Debug.LogError("Avatar Positioner needs a tracked head transform.", this);
                enabled = false;
            }
        }

        // Use LateUpdate so tracking/network scripts get a chance to move the player transforms first.
        // Then this script copies those finished positions onto the visible avatar parts.
        void LateUpdate()
        {
            ResolvePoseInputs();

            if (activeHeadPosition != null)
            {
                Vector3 headPosition = activeHeadPosition.position;

                if (avatarRoot != null)
                    avatarRoot.position = headPosition;

                MovePart(headObject, activeHeadPosition, headOffset, headRotationOffset, GetRotationAdjust(headRotationAdjust, hmdHeadRotationAdjust));
                ApplyBody();
                MovePart(leftHandObject, activeLeftHandPosition, leftHandOffset, leftHandRotationOffset, GetRotationAdjust(leftHandRotationAdjust, hmdLeftHandRotationAdjust));
                MovePart(rightHandObject, activeRightHandPosition, rightHandOffset, rightHandRotationOffset, GetRotationAdjust(rightHandRotationAdjust, hmdRightHandRotationAdjust));
            }
        }

        void ResolvePoseInputs()
        {
            if (networkPlayer == null)
                networkPlayer = GetComponent<XRINetworkPlayer>();

            if (networkPlayer != null && networkPlayer.IsSpawned)
                isSelf = networkPlayer.IsOwner;

            if (networkPlayer != null)
            {
                if (playerHeadPosition == null)
                    playerHeadPosition = networkPlayer.head;
                if (playerLeftHandPosition == null)
                    playerLeftHandPosition = networkPlayer.leftHand;
                if (playerRightHandPosition == null)
                    playerRightHandPosition = networkPlayer.rightHand;
            }

            if (isSelf)
            {
                if (networkPlayer != null && networkPlayer.IsSpawned)
                {
                    activeHeadPosition = playerHeadPosition;
                    activeLeftHandPosition = playerLeftHandPosition;
                    activeRightHandPosition = playerRightHandPosition;
                }
                else
                {
                    if (localHandPoseSource == null)
                        localHandPoseSource = FindFirstObjectByType<LocalHandPoseSource>();

                    if (selfHeadPosition == null)
                    {
                        XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
                        if (xrOrigin != null && xrOrigin.Camera != null)
                            selfHeadPosition = xrOrigin.Camera.transform;
                    }

                    if (localHandPoseSource != null)
                    {
                        selfLeftHandPosition = localHandPoseSource.activeLeftHandPosition;
                        selfRightHandPosition = localHandPoseSource.activeRightHandPosition;
                    }
                    else
                    {
                        if (selfLeftHandPosition == null && networkPlayer != null)
                            selfLeftHandPosition = networkPlayer.leftHand;
                        if (selfRightHandPosition == null && networkPlayer != null)
                            selfRightHandPosition = networkPlayer.rightHand;
                    }

                    activeHeadPosition = selfHeadPosition;
                    activeLeftHandPosition = selfLeftHandPosition;
                    activeRightHandPosition = selfRightHandPosition;
                }
            }
            else
            {
                activeHeadPosition = playerHeadPosition;
                activeLeftHandPosition = playerLeftHandPosition;
                activeRightHandPosition = playerRightHandPosition;
            }
        }

        Vector3 GetRotationAdjust(Vector3 standardAdjust, Vector3 hmdAdjust)
        {
            Vector3 rotationAdjust = standardAdjust;

            if (networkPlayer != null)
            {
                XRPlatformType platformType = (XRPlatformType)networkPlayer.platformType.Value;
                if (platformType == XRPlatformType.Quest || platformType == XRPlatformType.AndroidXR)
                    rotationAdjust += hmdAdjust;
            }

            return rotationAdjust;
        }

        void MovePart(Transform part, Transform source, Vector3 offset, Quaternion rotationOffset, Vector3 rotationAdjust)
        {
            if (part != null && source != null)
            {
                part.position = source.position + source.rotation * offset;
                part.rotation = source.rotation * Quaternion.Euler(rotationAdjust) * rotationOffset;
            }
        }

        void ApplyBody()
        {
            if (bodyObject != null && activeHeadPosition != null)
            {
                Quaternion bodySourceRotation = GetBodyRotation();
                bodyObject.position = activeHeadPosition.position + bodySourceRotation * bodyOffset;

                Quaternion targetRotation;
                if (bodyUsesHeadYawOnly)
                {
                    float currentYaw = bodyObject.eulerAngles.y;
                    float headYaw = activeHeadPosition.eulerAngles.y;
                    if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, headYaw)) >= bodyTurnThreshold)
                        targetBodyYaw = headYaw;

                    targetRotation = Quaternion.Euler(0f, targetBodyYaw, 0f);
                }
                else
                {
                    targetRotation = activeHeadPosition.rotation;
                }

                bodyObject.rotation = Quaternion.Slerp(
                    bodyObject.rotation,
                    targetRotation * Quaternion.Euler(GetRotationAdjust(bodyRotationAdjust, hmdBodyRotationAdjust)) * bodyRotationOffset,
                    Time.deltaTime * bodyTurnSpeed);
            }
        }

        Quaternion GetBodyRotation()
        {
            Quaternion bodyRotationSource = Quaternion.identity;

            if (activeHeadPosition != null)
            {
                if (bodyUsesHeadYawOnly)
                    bodyRotationSource = Quaternion.Euler(0f, activeHeadPosition.eulerAngles.y, 0f);
                else
                    bodyRotationSource = activeHeadPosition.rotation;
            }

            return bodyRotationSource;
        }

        static Quaternion GetRotationOffset(Quaternion sourceRotation, Transform part)
        {
            Quaternion rotationOffset = Quaternion.identity;

            if (part != null)
                rotationOffset = Quaternion.Inverse(sourceRotation) * part.rotation;

            return rotationOffset;
        }

        static Quaternion GetRotationOffset(Transform source, Transform part)
        {
            Quaternion rotationOffset = Quaternion.identity;

            if (source != null && part != null)
                rotationOffset = GetRotationOffset(source.rotation, part);

            return rotationOffset;
        }
    }
}
