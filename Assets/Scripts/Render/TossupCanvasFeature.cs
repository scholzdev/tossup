using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Tossup.UI
{
    // Owns the canvas draw hook. Scene/preview cameras never update the game's UI state.
    public sealed class TossupCanvasFeature : ScriptableRendererFeature
    {
        CanvasPass pass;

        public override void Create()
        {
            pass = new CanvasPass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Game && camera.TryGetComponent<TossupApp>(out var app) && app.isActiveAndEnabled)
                renderer.EnqueuePass(pass);
        }

        sealed class CanvasPass : ScriptableRenderPass
        {
            sealed class PassData
            {
                public UnityGfxBackend Backend;
                public UniversalCameraData Camera;
                public TextureHandle Target;
                public Matrix4x4 Projection;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var camera = frameData.Get<UniversalCameraData>();
                if (!camera.camera.TryGetComponent<TossupApp>(out var app)) return;
                var backend = app.PrepareCanvas();
                if (backend == null) return;
                var resources = frameData.Get<UniversalResourceData>();
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Tossup canvas", out var data))
                {
                    data.Backend = backend;
                    data.Camera = camera;
                    data.Target = resources.activeColorTexture;
                    data.Projection = Matrix4x4.Ortho(0, Screen.width, Screen.height, 0, -1, 1);
                    // Alpha blending must load the camera clear and retain primitive order.
                    builder.SetRenderAttachment(data.Target, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                    {
                        var projection = GL.GetGPUProjectionMatrix(d.Projection,
                            d.Camera.IsRenderTargetProjectionMatrixFlipped(d.Target));
                        d.Backend.Render(context.cmd, projection);
                    });
                }
            }
        }
    }
}
