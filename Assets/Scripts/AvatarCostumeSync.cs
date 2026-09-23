using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRClassroom
{
    /// <summary>
    /// this script was written by codex (which is why it's more complicated than it needs to be)
    /// would benefit from detailed human code review
    /// Keeps a network avatar's costume matched to the local player's offline avatar preview.
    /// Costume identity is intentionally just the chosen garment GameObject name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AvatarCostumeSync : NetworkBehaviour
    {
        public AvatarPositioner avatarPositioner;

        readonly NetworkVariable<FixedString64Bytes> headGarmentName = new(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<FixedString64Bytes> bodyGarmentName = new(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (avatarPositioner == null)
                avatarPositioner = GetComponent<AvatarPositioner>();

            headGarmentName.OnValueChanged += HeadGarmentChanged;
            bodyGarmentName.OnValueChanged += BodyGarmentChanged;

            if (IsOwner)
                CopyCostumeFromOfflineAvatar();

            ApplyHead(headGarmentName.Value.ToString());
            ApplyBody(bodyGarmentName.Value.ToString());
        }

        public override void OnNetworkDespawn()
        {
            headGarmentName.OnValueChanged -= HeadGarmentChanged;
            bodyGarmentName.OnValueChanged -= BodyGarmentChanged;
            base.OnNetworkDespawn();
        }

        public void SetHead(string garmentName)
        {
            if (IsOwner && !string.IsNullOrEmpty(garmentName))
            {
                headGarmentName.Value = garmentName;
                ApplyHead(garmentName);
            }
        }

        public void SetBody(string garmentName)
        {
            if (IsOwner && !string.IsNullOrEmpty(garmentName))
            {
                bodyGarmentName.Value = garmentName;
                ApplyBody(garmentName);
            }
        }

        void CopyCostumeFromOfflineAvatar()
        {
            CostumeCloset costumeCloset = FindFirstObjectByType<CostumeCloset>();
            if (costumeCloset != null && costumeCloset.avatarPositioner != null)
            {
                if (costumeCloset.avatarPositioner.headObject != null)
                    SetHead(costumeCloset.avatarPositioner.headObject.name);

                if (costumeCloset.avatarPositioner.bodyObject != null)
                    SetBody(costumeCloset.avatarPositioner.bodyObject.name);
            }
        }

        void HeadGarmentChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
        {
            ApplyHead(newName.ToString());
        }

        void BodyGarmentChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
        {
            ApplyBody(newName.ToString());
        }

        void ApplyHead(string garmentName)
        {
            if (avatarPositioner != null && avatarPositioner.headObject != null && !string.IsNullOrEmpty(garmentName))
            {
                CostumeCloset costumeCloset = FindFirstObjectByType<CostumeCloset>();
                GameObject headGarment = costumeCloset == null ? null : costumeCloset.FindGarment(garmentName, GarmentType.Head);

                if (headGarment != null && avatarPositioner.headObject.name != headGarment.name)
                {
                    Transform oldHead = avatarPositioner.headObject;
                    Vector3 garmentWorldScale = headGarment.transform.lossyScale;

                    GameObject newHead = Instantiate(headGarment, oldHead.position, oldHead.rotation, oldHead.parent);
                    newHead.name = headGarment.name;
                    // Keep the size the garment had in the closet rather than copying the previous head's
                    // local scale (which only worked when a garment kept its whole scale on its root).
                    newHead.transform.localScale = GarmentFit.LocalScaleForWorldScale(oldHead.parent, garmentWorldScale);

                    avatarPositioner.headObject = newHead.transform;
                    Destroy(oldHead.gameObject);
                }
            }
        }

        void ApplyBody(string garmentName)
        {
            if (avatarPositioner != null && avatarPositioner.bodyObject != null && !string.IsNullOrEmpty(garmentName))
            {
                CostumeCloset costumeCloset = FindFirstObjectByType<CostumeCloset>();
                GameObject bodyGarment = costumeCloset == null ? null : costumeCloset.FindGarment(garmentName, GarmentType.Body);

                if (bodyGarment != null && avatarPositioner.bodyObject.name != bodyGarment.name)
                {
                    Transform oldBody = avatarPositioner.bodyObject;
                    Vector3 garmentWorldScale = bodyGarment.transform.lossyScale;

                    GameObject newBody = Instantiate(bodyGarment, oldBody.position, oldBody.rotation, oldBody.parent);
                    newBody.name = bodyGarment.name;
                    // Keep the size the garment had in the closet rather than copying the previous body's
                    // local scale (which only worked when a garment kept its whole scale on its root).
                    newBody.transform.localScale = GarmentFit.LocalScaleForWorldScale(oldBody.parent, garmentWorldScale);

                    avatarPositioner.bodyObject = newBody.transform;
                    Destroy(oldBody.gameObject);
                }
            }
        }
    }
}
