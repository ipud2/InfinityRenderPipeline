﻿using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace InfinityTech.Rendering.GPUResource
{
    public enum EDepthBits
    {
        None = 0,
        Depth8 = 8,
        Depth16 = 16,
        Depth24 = 24,
        Depth32 = 32
    }

    public enum EMSAASamples
    {
        None = 1,
        MSAA2x = 2,
        MSAA4x = 4,
        MSAA8x = 8
    }

    public struct FBufferDescription
    {
        public string name;

        public int count;
        public int stride;
        public ComputeBufferType type;

        public FBufferDescription(int count, int stride) : this()
        {
            this.count = count;
            this.stride = stride;
            type = ComputeBufferType.Default;
        }

        public FBufferDescription(int count, int stride, ComputeBufferType type) : this()
        {
            this.type = type;
            this.count = count;
            this.stride = stride;
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 23 + count;
            hashCode = hashCode * 23 + stride;
            hashCode = hashCode * 23 + (int)type;
            return hashCode;
        }
    }

    public struct FTextureDescription
    {
        public string name;

        public int width;
        public int height;
        public int slices;
        public EDepthBits depthBufferBits;
        public GraphicsFormat colorFormat;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public TextureDimension dimension;
        public bool enableRandomWrite;
        public bool useMipMap;
        public bool autoGenerateMips;
        public bool isShadowMap;
        public int anisoLevel;
        public float mipMapBias;
        public bool enableMSAA;
        public bool bindTextureMS;
        public EMSAASamples msaaSamples;
        public bool clearBuffer;
        public Color clearColor;

        public FTextureDescription(int Width, int Height) : this()
        {
            width = Width;
            height = Height;
            slices = 1;

            isShadowMap = false;
            enableRandomWrite = false;

            msaaSamples = EMSAASamples.None;
            depthBufferBits = EDepthBits.None;
            wrapMode = TextureWrapMode.Repeat;
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 23 + width;
            hashCode = hashCode * 23 + height;
            hashCode = hashCode * 23 + slices;
            hashCode = hashCode * 23 + mipMapBias.GetHashCode();
            hashCode = hashCode * 23 + (int)depthBufferBits;
            hashCode = hashCode * 23 + (int)colorFormat;
            hashCode = hashCode * 23 + (int)filterMode;
            hashCode = hashCode * 23 + (int)wrapMode;
            hashCode = hashCode * 23 + (int)dimension;
            hashCode = hashCode * 23 + anisoLevel;
            hashCode = hashCode * 23 + (enableRandomWrite ? 1 : 0);
            hashCode = hashCode * 23 + (useMipMap ? 1 : 0);
            hashCode = hashCode * 23 + (autoGenerateMips ? 1 : 0);
            hashCode = hashCode * 23 + (isShadowMap ? 1 : 0);
            hashCode = hashCode * 23 + (bindTextureMS ? 1 : 0);
            return hashCode;
        }

        public static implicit operator RenderTextureDescriptor(in FTextureDescription description)
        {
            RenderTextureDescriptor rtDescription = new RenderTextureDescriptor(description.width, description.height, description.colorFormat, (int)description.depthBufferBits, -1);
            rtDescription.vrUsage = VRTextureUsage.None;
            rtDescription.volumeDepth = description.slices;
            rtDescription.useMipMap = description.useMipMap;
            rtDescription.dimension = description.dimension;
            rtDescription.stencilFormat = GraphicsFormat.None;
            rtDescription.bindMS = description.bindTextureMS;
            rtDescription.depthStencilFormat = GraphicsFormat.None;
            rtDescription.memoryless = RenderTextureMemoryless.None;
            rtDescription.msaaSamples = (int)description.msaaSamples;
            rtDescription.shadowSamplingMode = ShadowSamplingMode.None;
            rtDescription.autoGenerateMips = description.autoGenerateMips;
            rtDescription.autoGenerateMips = description.autoGenerateMips;
            rtDescription.enableRandomWrite = description.enableRandomWrite;
            return rtDescription;
        }
    }

    public struct FBufferRef
    {
        internal int handle;
        public ComputeBuffer buffer;

        public FBufferRef(in int handle, ComputeBuffer buffer) 
        { 
            this.handle = handle;
            this.buffer = buffer; 
        }

        public static implicit operator ComputeBuffer(in FBufferRef bufferRef) => bufferRef.buffer;
    }

    public struct FTextureRef
    {
        internal int handle;
        public RTHandle texture;

        internal FTextureRef(in int handle, RTHandle texture) 
        {
            this.handle = handle;
            this.texture = texture; 
        }

        public static implicit operator RTHandle(in FTextureRef textureRef) => textureRef.texture;
    }

    public abstract class FGPUResourceCache<Type> where Type : class
    {
        protected Dictionary<int, List<Type>> m_ResourcePool = new Dictionary<int, List<Type>>(64);

        abstract protected void ReleaseInternalResource(Type res);
        abstract protected string GetResourceName(Type res);
        abstract protected string GetResourceTypeName();

        public bool Pull(in int hashCode, out Type resource)
        {
            if (m_ResourcePool.TryGetValue(hashCode, out var list) && list.Count > 0)
            {
                //resource = list[0];
                //list.RemoveAt(0);
                resource = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return true;
            }

            resource = null;
            return false;
        }

        public void Push(in int hash, Type resource)
        {
            if (!m_ResourcePool.TryGetValue(hash, out var list))
            {
                list = new List<Type>();
                m_ResourcePool.Add(hash, list);
            }

            list.Add(resource);
        }

        public void Dispose()
        {
            foreach (var kvp in m_ResourcePool)
            {
                foreach (Type resource in kvp.Value)
                {
                    ReleaseInternalResource(resource);
                }
            }
        }
    }

    public class FBufferCache : FGPUResourceCache<ComputeBuffer>
    {
        protected override void ReleaseInternalResource(ComputeBuffer res)
        {
            res.Release();
        }

        protected override string GetResourceName(ComputeBuffer res)
        {
            return "BufferNameNotAvailable";
        }

        override protected string GetResourceTypeName()
        {
            return "Buffer";
        }
    }

    public class FTextureCache : FGPUResourceCache<RTHandle>
    {
        protected override void ReleaseInternalResource(RTHandle res)
        {
            res.Release();
        }

        protected override string GetResourceName(RTHandle res)
        {
            return res.name;
        }

        override protected string GetResourceTypeName()
        {
            return "Texture";
        }
    }
}
