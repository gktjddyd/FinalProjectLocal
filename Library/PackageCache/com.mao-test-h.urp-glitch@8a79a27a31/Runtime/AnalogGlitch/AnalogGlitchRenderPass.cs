// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/AnalogGlitch.cs

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // GraphicsFormat 관련

namespace URPGlitch.Runtime.AnalogGlitch
{
    sealed class AnalogGlitchRenderPass : ScriptableRenderPass, IDisposable
    {
        const string RenderPassName = "AnalogGlitch RenderPass";

        // Material Properties
        static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
        static readonly int VerticalJumpID = Shader.PropertyToID("_VerticalJump");
        static readonly int HorizontalShakeID = Shader.PropertyToID("_HorizontalShake");
        static readonly int ColorDriftID = Shader.PropertyToID("_ColorDrift");

        readonly ProfilingSampler _profilingSampler;
        readonly Material _glitchMaterial;
        readonly AnalogGlitchVolume _volume;

        // RTHandle 및 임시 RT ID (정수값)을 별도로 관리합니다.
        RTHandle _mainFrame;
        int _mainFrameID; // 예: Shader.PropertyToID("_MainFrame")
        float _verticalJumpTime;

        bool isActive =>
            _glitchMaterial != null &&
            _volume != null &&
            _volume.IsActive;

        public AnalogGlitchRenderPass(Shader shader)
        {
            try{
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                _profilingSampler = new ProfilingSampler(RenderPassName);
                _glitchMaterial = CoreUtils.CreateEngineMaterial(shader);

                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<AnalogGlitchVolume>();

                // 임시 RT에 사용할 ID를 생성 (쉐이더 프로퍼티 이름과 동일하게)
                _mainFrameID = Shader.PropertyToID("_MainFrame");

                // RTHandle 생성 (placeholder 역할로 scaleFactor를 Vector2.one 사용)
                _mainFrame = RTHandles.Alloc(
                scaleFactor: Vector2.one,
                filterMode: FilterMode.Bilinear,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                useDynamicScale: true,
                name: "_MainFrame" );
            }
            catch(NullReferenceException)
            {
                return;
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(_glitchMaterial);
            RTHandles.Release(_mainFrame);
        }

        // 렌더링 로직 구현
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 카메라 타깃 프로퍼티 변경: cameraColorTargetHandle 사용
            var isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
            var isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;
            if (!isActive || !isPostProcessEnabled || isSceneViewCamera)
            {
                return;
            }

            var cmd = CommandBufferPool.Get(RenderPassName);
            cmd.Clear();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // 변경된 카메라 타깃 프로퍼티 사용
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.depthBufferBits = 0;

                // 임시 RT 할당 - 별도 관리한 정수 ID(_mainFrameID) 사용
                cmd.GetTemporaryRT(_mainFrameID, cameraTargetDescriptor);

                // RTHandle은 암시적으로 RenderTargetIdentifier로 변환됨
                // 또는 명시적 캐스팅: (RenderTargetIdentifier)_mainFrame
                cmd.Blit(source, (RenderTargetIdentifier)_mainFrame);

                var scanLineJitter = _volume.scanLineJitter.value;
                var verticalJump = _volume.verticalJump.value;
                var horizontalShake = _volume.horizontalShake.value;
                var colorDrift = _volume.colorDrift.value;

                _verticalJumpTime += Time.deltaTime * verticalJump * 11.3f;
                var slThresh = Mathf.Clamp01(1.0f - scanLineJitter * 1.2f);
                var slDisp = 0.002f + Mathf.Pow(scanLineJitter, 3) * 0.05f;
                _glitchMaterial.SetVector(ScanLineJitterID, new Vector2(slDisp, slThresh));

                var vj = new Vector2(verticalJump, _verticalJumpTime);
                _glitchMaterial.SetVector(VerticalJumpID, vj);
                _glitchMaterial.SetFloat(HorizontalShakeID, horizontalShake * 0.2f);

                var cd = new Vector2(colorDrift * 0.04f, Time.time * 606.11f);
                _glitchMaterial.SetVector(ColorDriftID, cd);

                cmd.SetGlobalTexture(MainTexID, (RenderTargetIdentifier)_mainFrame);
                cmd.Blit((RenderTargetIdentifier)_mainFrame, source, _glitchMaterial);
                cmd.ReleaseTemporaryRT(_mainFrameID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
