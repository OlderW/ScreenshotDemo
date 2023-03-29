using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class ScreenFrameGenerator 
{
    public byte[] tempYUVData;

    public int byteSize = 0;
    public int uintSize = 0;
    public ComputeShader shader;
    RenderTexture rt;
    ComputeBuffer m_outputBuffer;

    uint[] m_yuv420RawData;
    byte[] result;

    private Texture2D texture2D;

    public int curScreenWidth = 0;
    public int curScreenHeight = 0;
    public int realWidth = 0;
    public int realHeight = 0;

    public ScreenFrameGenerator()
    {
        RefreshWidthHeight();
    }

    public void RefreshWidthHeight()
    {
        //取16的倍数否则一些分辨率下显示异常
        curScreenWidth = Screen.width;
        curScreenHeight = Screen.height;
        int w = Screen.width % 16;
        realWidth = Screen.width - w;
        int h = Screen.height % 16;
        realHeight = Screen.height - h;

        texture2D = new Texture2D(realWidth, realHeight, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
        if (m_outputBuffer != null)
        {
            m_outputBuffer.Release();
            m_outputBuffer = null;
            m_yuv420RawData = null;
            result = null;
        }
    }

    public byte[] GetFrameBuffer()
    {

        OnGetNextFrameSize();

        return tempYUVData;
    }


    private int OnGetNextFrameSize()
    {
        tempYUVData = GetYUVData(realWidth, realHeight);
        return tempYUVData.Length;
    }


    private byte[] GetYUVData(int width, int height)
    {
        byteSize = width * height * 3 / 2;
        uintSize = byteSize / 4;

        if (m_outputBuffer == null)
        {
            m_outputBuffer = new ComputeBuffer(uintSize, sizeof(uint));

            if (m_yuv420RawData == null)
            {
                m_yuv420RawData = new uint[uintSize];
            }
            m_outputBuffer.SetData(m_yuv420RawData);
        }

        if (m_yuv420RawData == null || uintSize != m_yuv420RawData.Length)
        {
            if (m_outputBuffer != null)
            {
                m_outputBuffer.Release();
                m_outputBuffer = null;
            }
            m_outputBuffer = new ComputeBuffer(uintSize, sizeof(uint));

            m_outputBuffer.SetData(m_yuv420RawData);
        }


        int handle = shader.FindKernel("ReadYUV420Data_Reverse");

        if (rt != null)
        {
            rt.DiscardContents();

            if (rt.width != width || rt.height != height)
            {

                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }
        }

        if (rt == null)
        {
            rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.name = "ScreenFrameLiving";
        }

        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);

        FormatConversionUtil.ScreenDownTest(rt);

        shader.SetTexture(handle, "InputTexture", rt);
        shader.SetBuffer(handle, "OutputBuffer", m_outputBuffer);
        shader.SetBool("useLinear", true);

        shader.Dispatch(handle, width / 8, height / 8, 1);

        if (result == null || result.Length != byteSize)
        {
            result = new byte[byteSize];
        }

        m_outputBuffer.GetData(m_yuv420RawData);

        NativeBridge.CopyUintToByte(m_yuv420RawData, byteSize, result);
        FormatConversionUtil.YUVDownTest(rt,result);
        return result;
    }

    public void Release()
    {
        if (rt != null)
        {
            RenderTexture.ReleaseTemporary(rt);
            rt = null;
            //RenderTexture.ReleaseTemporary(rt);
        }

        if (m_outputBuffer != null)
        {
            m_outputBuffer.Release();
            m_outputBuffer = null;
        }
    }
}
