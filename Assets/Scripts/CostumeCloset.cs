using XRClassroom;
using UnityEngine;
using XRMultiplayer;


/// <summary>
/// this script is mostly human-authored and more trustworthy
/// </summary>

public class CostumeCloset : MonoBehaviour
{
    public AvatarPositioner avatarPositioner;
    public Transform headParent;
    public Transform bodyParent;

    private AvatarCostumeSync onlineAvatarSync;//this doesn't exist when we're not online, so we can't connect it in the inspector


    public GameObject FindGarment(string garmentName, GarmentType garmentType)
    {
        foreach (WearableGarment wearableGarment in GetComponentsInChildren<WearableGarment>(true))
        {
            if (wearableGarment.garmentType == garmentType && wearableGarment.garmentPrefab != null && wearableGarment.garmentPrefab.name == garmentName)
                return wearableGarment.garmentPrefab;
        }

        return null;
    }

    public void AttemptWear(GameObject garment, GarmentType garmentType)
    {
        // The garment is authored to look right where it hangs in the closet. Capture that world-space
        // size now, before we clone and reparent it, so the worn copy can be scaled to match.
        Vector3 garmentWorldScale = garment.transform.lossyScale;

        // offline behavior -- do this always because its the version of you that you see
        GameObject newGarment = Instantiate(garment);
        newGarment.name = garment.name;

        // online behavior
        // call avatar costume sync to make changes to the version of you that others see
        if (XRINetworkGameManager.Connected.Value)
        {
            if (onlineAvatarSync == null)
            {
                foreach (AvatarCostumeSync avatarSync in FindObjectsByType<AvatarCostumeSync>(FindObjectsSortMode.None))
                {
                    if (avatarSync.IsOwner)
                    {
                        onlineAvatarSync = avatarSync;
                    }
                }
            }
        }

        if (garmentType == GarmentType.Head)
        {
            if (XRINetworkGameManager.Connected.Value && onlineAvatarSync != null)
            {
                onlineAvatarSync.SetHead(garment.name);
            }
            WearHead(newGarment, garmentWorldScale);// offline behavior -- do this always because its the version of you that you see
        }

        if(garmentType == GarmentType.Body)
        {
            if (XRINetworkGameManager.Connected.Value && onlineAvatarSync != null)
            {
                onlineAvatarSync.SetBody(garment.name);
            }
            WearBody(newGarment, garmentWorldScale);// offline behavior -- do this always because its the version of you that you see
        }

    }

    private void WearHead(GameObject head, Vector3 worldScale)
    {
        Transform oldHead = avatarPositioner.headObject;

        head.transform.SetParent(headParent, true);
        head.transform.position = oldHead.position;
        // Match the size the garment had in the closet instead of copying the previous head's local
        // scale. Copying local scale only worked when every garment kept its whole scale on its root.
        head.transform.localScale = GarmentFit.LocalScaleForWorldScale(headParent, worldScale);

        avatarPositioner.headObject = head.transform; //is doing this via transforms rather than whole gameobjects going to cause a problem?
        Destroy(oldHead.gameObject);
    }

    private void WearBody(GameObject body, Vector3 worldScale)
    {
        Transform oldBody = avatarPositioner.bodyObject;

        body.transform.SetParent(bodyParent, true);
        body.transform.position = oldBody.position;
        body.transform.localScale = GarmentFit.LocalScaleForWorldScale(bodyParent, worldScale);

        avatarPositioner.bodyObject = body.transform;
        Destroy(oldBody.gameObject);
    }
}
