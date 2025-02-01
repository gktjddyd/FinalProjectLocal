using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kino
{
    // Define the custom volume component
    [System.Serializable, VolumeComponentMenu("Custom/Analog Glitch")]
    public class AnalogGlitchPostProcess : VolumeComponent, IPostProcessComponent
    {
        // Public parameters to control the glitch effect
        public ClampedFloatParameter scanLineJitter = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter verticalJump = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter horizontalShake = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter colorDrift = new ClampedFloatParameter(0f, 0f, 1f);

        // Determines if the effect should be rendered
        public bool IsActive() => scanLineJitter.value > 0f || verticalJump.value > 0f || horizontalShake.value > 0f || colorDrift.value > 0f;

        public bool IsTileCompatible() => false;
    }

    // Define the custom render pass
    public class AnalogGlitchPostProcessRenderer : ScriptableRendererFeature
    {
        class AnalogGlitchRenderPass : ScriptableRenderPass
        {
            private Material _material;
            private AnalogGlitchPostProcess _component;

            public AnalogGlitchRenderPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            public void Setup(AnalogGlitchPostProcess component)
            {
                _component = component;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null || _component == null) return;

                var cmd = CommandBufferPool.Get("Analog Glitch");

                // Source와 Destination을 설정
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var destination = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // 글리치 효과 적용
                _material.SetFloat("_ScanLineJitter", _component.scanLineJitter.value);
                _material.SetFloat("_VerticalJump", _component.verticalJump.value);
                _material.SetFloat("_HorizontalShake", _component.horizontalShake.value);
                _material.SetFloat("_ColorDrift", _component.colorDrift.value);

                // Blit 호출 수정
                Blit(cmd, source, destination, _material);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        [SerializeField] private Shader _shader;
        private Material _material;
        private AnalogGlitchRenderPass _renderPass;

        public override void Create()
        {
            if (_shader == null)
            {
                Debug.LogError("Shader not assigned for Analog Glitch Post Process.");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _renderPass = new AnalogGlitchRenderPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var volumeStack = VolumeManager.instance.stack;
            var component = volumeStack.GetComponent<AnalogGlitchPostProcess>();

            if (component != null && component.IsActive())
            {
                _renderPass.Setup(component);
                renderer.EnqueuePass(_renderPass);
            }
        }
    }
}
