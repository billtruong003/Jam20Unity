using UnityEngine.Events;
#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    class VFX_UNI_OE_LightOFF : VFXOutputEventAbstractHandler
    {
        public override bool canExecuteInEditor => false;

        public GameObject lightGameObject;

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if (lightGameObject != null)
                lightGameObject.SetActive(false);

        }
    }
}