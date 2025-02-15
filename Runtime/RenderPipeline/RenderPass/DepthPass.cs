using UnityEngine;
using UnityEngine.Rendering;
using InfinityTech.Rendering.RDG;
using InfinityTech.Rendering.GPUResource;
using InfinityTech.Rendering.MeshPipeline;

namespace InfinityTech.Rendering.Pipeline
{
    internal static class FDepthPassUtilityData
    {
        internal static string PassName = "Depth";
        internal static string TextureName = "DepthTexture";
    }

    public partial class InfinityRenderPipeline
    {
        struct FDepthPassData
        {
            public Camera camera;
            public FRDGTextureRef depthTexture;
            public CullingResults cullingResults;
            public FMeshPassProcessor meshPassProcessor;
        }

        void RenderDepth(Camera camera, in FCullingData cullingData, in CullingResults cullingResults)
        {
            FTextureDescription depthDescription = new FTextureDescription(camera.pixelWidth, camera.pixelHeight) { clearBuffer = true, dimension = TextureDimension.Tex2D, enableMSAA = false, bindTextureMS = false, name = FDepthPassUtilityData.TextureName, depthBufferBits = EDepthBits.Depth32 };

            FRDGTextureRef depthTexture = m_GraphBuilder.ScopeTexture(InfinityShaderIDs.DepthBuffer, depthDescription);

            //Add DepthPass
            using (FRDGPassRef passRef = m_GraphBuilder.AddPass<FDepthPassData>(FDepthPassUtilityData.PassName, ProfilingSampler.Get(CustomSamplerId.RenderDepth)))
            {
                //Setup Phase
                ref FDepthPassData passData = ref passRef.GetPassData<FDepthPassData>();
                passData.camera = camera;
                passData.cullingResults = cullingResults;
                passData.meshPassProcessor = m_DepthMeshProcessor;
                passData.depthTexture = passRef.UseDepthBuffer(depthTexture, EDepthAccess.ReadWrite);
                
                m_DepthMeshProcessor.DispatchSetup(cullingData, new FMeshPassDesctiption(2450, 2999));

                //Execute Phase
                passRef.SetExecuteFunc((ref FDepthPassData passData, ref FRDGContext graphContext) =>
                {
                    //MeshDrawPipeline
                    passData.meshPassProcessor.DispatchDraw(graphContext, 0);

                    //UnityDrawPipeline
                    FilteringSettings filteringSettings = new FilteringSettings
                    {
                        renderingLayerMask = 1,
                        layerMask = passData.camera.cullingMask,
                        renderQueueRange = new RenderQueueRange(2450, 2999),
                    };
                    DrawingSettings drawingSettings = new DrawingSettings(InfinityPassIDs.DepthPass, new SortingSettings(passData.camera) { criteria = SortingCriteria.QuantizedFrontToBack })
                    {
                        enableInstancing = true,
                        enableDynamicBatching = false
                    };
                    graphContext.renderContext.DrawRenderers(passData.cullingResults, ref drawingSettings, ref filteringSettings);
                });
            }
        }
    }
}