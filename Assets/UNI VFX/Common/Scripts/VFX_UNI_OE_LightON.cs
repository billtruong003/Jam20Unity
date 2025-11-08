using UnityEngine.Events;
#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    class VFX_UNI_OE_LightON : VFXOutputEventAbstractHandler
    {
        public override bool canExecuteInEditor => false;

        public GameObject lightGameObject;
        public float brightnessScale = 1.0f;
        static readonly int k_Color = Shader.PropertyToID("color");

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if (lightGameObject != null)
                lightGameObject.SetActive(true);

            var color = eventAttribute.GetVector3(k_Color);
            var intensity = color.magnitude;
            var c = new Color(color.x, color.y, color.z) / intensity;
            intensity *= brightnessScale;

#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
            var hdlight = lightGameObject.GetComponent<HDAdditionalLightData>();
            hdlight.SetColor(c);
            hdlight.SetIntensity(intensity);
#else
            var light = lightGameObject.GetComponent<Light>();
            light.color = c;
            light.intensity = intensity;
#endif
        }

    }
}