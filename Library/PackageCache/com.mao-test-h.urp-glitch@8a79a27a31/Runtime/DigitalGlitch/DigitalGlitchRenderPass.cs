// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/DigitalGlitch.cs

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // GraphicsFormat 관련

namespace URPGlitch.Runtime.DigitalGlitch
{
    sealed class DigitalGlitchRenderPass : ScriptableRenderPass, IDisposable
    {
        const string RenderPassName = "DigitalGlitch RenderPass";

        // Material Properties
        static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
        static readonly int TrashTexID = Shader.PropertyToID("_TrashTex");
        static readonly int IntensityID = Shader.PropertyToID("_Intensity");

        readonly ProfilingSampler _profilingSampler;
        readonly System.Random _random;

        readonly Material _glitchMaterial;
        readonly Texture2D _noiseTexture;
        readonly DigitalGlitchVolume _volume;

        // RTHandle들 및 임시 RT ID 값을 관리합니다.
        RTHandle _mainFrame;
        RTHandle _trashFrame1;
        RTHandle _trashFrame2;

        int _mainFrameID;
        int _trashFrame1ID;
        int _trashFrame2ID;

        bool isActive =>
            _glitchMaterial != null &&
            _volume != null &&
            _volume.IsActive;

        public DigitalGlitchRenderPass(Shader shader)
        {
            try{
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                _profilingSampler = new ProfilingSampler(RenderPassName);
                _random = new System.Random();
                _glitchMaterial = CoreUtils.CreateEngineMaterial(shader);

                _noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false)
                {
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };

                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<DigitalGlitchVolume>();

                // 임시 RT에 사용할 ID 생성
                _mainFrameID = Shader.PropertyToID("_MainFrame");
                _trashFrame1ID = Shader.PropertyToID("_TrashFrame1");
             _trashFrame2ID = Shader.PropertyToID("_TrashFrame2");

                // RTHandle 생성 (placeholder 역할로 Vector2.one 사용)
                _mainFrame = RTHandles.Alloc(
                    scaleFactor: Vector2.one,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    useDynamicScale: true,
                    name: "_MainFrame"
                );
                _trashFrame1 = RTHandles.Alloc(
                    scaleFactor: Vector2.one,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    useDynamicScale: true,
                    name: "_TrashFrame1"
                );  
                _trashFrame2 = RTHandles.Alloc(
                    scaleFactor: Vector2.one,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    useDynamicScale: true,
                    name: "_TrashFrame2"
                );

            UpdateNoiseTexture();
            }
             catch(NullReferenceException)
            {
                return;
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(_glitchMaterial);
            CoreUtils.Destroy(_noiseTexture);
            RTHandles.Release(_mainFrame);
            RTHandles.Release(_trashFrame1);
            RTHandles.Release(_trashFrame2);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!isActive) return;

            var r = (float)_random.NextDouble();
            if (r > Mathf.Lerp(0.9f, 0.5f, _volume.intensity.value))
            {
                UpdateNoiseTexture();
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // cameraColorTarget -> cameraColorTargetHandle로 변경
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
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.depthBufferBits = 0;

                // 임시 RT 할당 (별도 관리한 ID 사용)
                cmd.GetTemporaryRT(_mainFrameID, cameraTargetDescriptor);
                cmd.GetTemporaryRT(_trashFrame1ID, cameraTargetDescriptor);
                cmd.GetTemporaryRT(_trashFrame2ID, cameraTargetDescriptor);

                // source → _mainFrame Blit
                cmd.Blit(source, (RenderTargetIdentifier)_mainFrame);

                var frameCount = Time.frameCount;
                if (frameCount % 13 == 0)
                    cmd.Blit(source, (RenderTargetIdentifier)_trashFrame1);
                if (frameCount % 73 == 0)
                    cmd.Blit(source, (RenderTargetIdentifier)_trashFrame2);

                var r = (float)_random.NextDouble();
                // 50% 확률에 따라 trash 프레임 선택
                var blitTrashHandle = r > 0.5f ? _trashFrame1 : _trashFrame2;
                cmd.SetGlobalFloat(IntensityID, _volume.intensity.value);
                cmd.SetGlobalTexture(NoiseTexID, _noiseTexture);
                cmd.SetGlobalTexture(MainTexID, (RenderTargetIdentifier)_mainFrame);
                cmd.SetGlobalTexture(TrashTexID, (RenderTargetIdentifier)blitTrashHandle);

                cmd.Blit((RenderTargetIdentifier)_mainFrame, source, _glitchMaterial);

                cmd.ReleaseTemporaryRT(_mainFrameID);
                cmd.ReleaseTemporaryRT(_trashFrame1ID);
                cmd.ReleaseTemporaryRT(_trashFrame2ID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void UpdateNoiseTexture()
        {
            var color = randomColor;
            for (var y = 0; y < _noiseTexture.height; y++)
            {
                for (var x = 0; x < _noiseTexture.width; x++)
                {
                    var r = (float)_random.NextDouble();
                    if (r > 0.89f)
                    {
                        color = randomColor;
                    }
                    _noiseTexture.SetPixel(x, y, color);
                }
            }
            _noiseTexture.Apply();
        }

        Color randomColor
        {
            get
            {
                var r = (float)_random.NextDouble();
                var g = (float)_random.NextDouble();
                var b = (float)_random.NextDouble();
                var a = (float)_random.NextDouble();
                return new Color(r, g, b, a);
            }
        }
    }
}
