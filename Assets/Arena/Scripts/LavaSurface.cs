using UnityEngine;

namespace GuerraDeEmpujes
{
    // Put on the solid lava collider, not on a trigger volume.
    public sealed class LavaSurface : MonoBehaviour
    {
        [Min(0)] public float damagePerSecond = 20f;
    }
}
