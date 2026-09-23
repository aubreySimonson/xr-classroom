using UnityEngine;

namespace XRClassroom
{
    /// <summary>
    /// Helpers for fitting a costume-closet garment onto an avatar.
    ///
    /// Closet garments are authored to look right where they hang in the closet. When a garment is
    /// worn it gets reparented under an avatar part socket, so its local scale has to be recomputed
    /// to keep the same world-space size it had on the hanger.
    ///
    /// The old approach copied the previous garment's local scale. That only produced the right
    /// result when every garment expressed its whole scale on its root object; a garment with scale
    /// on a child (for example a nested FBX import) ended up far too large or small.
    /// </summary>
    public static class GarmentFit
    {
        /// <summary>
        /// The local scale that reproduces <paramref name="worldScale"/> for an object parented under
        /// <paramref name="parent"/>. Assumes the parent chain has uniform, non-skewed scale, which is
        /// true for the XR Classroom avatar and closet hierarchies.
        /// </summary>
        public static Vector3 LocalScaleForWorldScale(Transform parent, Vector3 worldScale)
        {
            if (parent == null)
                return worldScale;

            Vector3 parentScale = parent.lossyScale;
            return new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? worldScale.x : worldScale.x / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? worldScale.y : worldScale.y / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? worldScale.z : worldScale.z / parentScale.z);
        }
    }
}
