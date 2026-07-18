using UnityEngine;

namespace ArenaFall.Utilities
{
    /// <summary>
    /// Produces a diagnostic identifier for Unity objects across supported Unity versions.
    /// Unity 6 removed GetInstanceID in favor of GetEntityId.
    /// </summary>
    public static class UnityObjectId
    {
        public static string ToString(Object unityObject)
        {
            if (unityObject == null)
                return string.Empty;

#if UNITY_6000_0_OR_NEWER
            return unityObject.GetEntityId().ToString();
#else
            return unityObject.GetInstanceID().ToString();
#endif
        }
    }
}
