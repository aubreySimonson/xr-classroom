using UnityEngine;
using XRClassroom;

public class WearableGarment : MonoBehaviour
{
    public CostumeCloset costumeCloset;
    public GameObject garmentPrefab;
    public GarmentType garmentType;

    public void TryOn()
    {
        costumeCloset.AttemptWear(garmentPrefab, garmentType);
    }
}
