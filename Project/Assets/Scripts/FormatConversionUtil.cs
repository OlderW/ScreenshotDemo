using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FormatConversionUtil : MonoBehaviour
{
    static double[,] YUV2RGB_CONVERT_MATRIX = new double[3, 3] { { 1, 0, 1.4022 }, { 1, -0.3456, -0.7145 }, { 1, 1.771, 0 } };

    public static int frame = 0;
    public static bool FrameActive = false;
    public static bool FrameByteActive = false;
    public static void ScreenDownTest(RenderTexture rt)
    {
        if (!FrameActive)
        {
            return;
        }

        if (rt == null || frame > 1000)
            return;
        frame++;

        if (frame > 200 && frame < 220)
        {
            int width = rt.width;
            int height = rt.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2D.Apply();
            byte[] vs = texture2D.EncodeToPNG();

            string dirc = @"D:\ScreenTest\";
            if (!Directory.Exists(dirc))
            {
                Directory.CreateDirectory(dirc);
            }
            string path = dirc + "/" + frame + ".png";
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(vs, 0, vs.Length);
            fileStream.Dispose();
            fileStream.Close();

        }

    }

    public static void YUVDownTest(RenderTexture rt, byte[] result)
    {
        if (!FrameByteActive)
        {
            return;
        }

        if (result == null || frame > 1000)
            return;
        frame++;

        if (frame > 200 && frame < 220)
        {
            int width = rt.width;
            int height = rt.height;

            string dirc = @"D:\ScreenTest\";
            if (!Directory.Exists(dirc))
            {
                Directory.CreateDirectory(dirc);
            }

            string path = Path.Combine(dirc, frame + ".bmp");
            byte[] bmp = new byte[width * height * 3];
            ConvertYUV2RGB(result, bmp, width, height);

            //if (!File.Exists(path))
            //{
            //    File.Create(dirc);
            //}
            Debug.Log("YUVDownTest  path:" + path);
            WriteBMP(bmp, width, height, path);
        }

    }

    public static void YUVDownTest2(RenderTexture rt)
    {
        if (frame > 1000)
            return;
        frame++;

        if (frame > 200 && frame < 220)
        {
            int width = rt.width;
            int height = rt.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2D.Apply();
            var rgbData = texture2D.GetRawTextureData();
            var result = RGB2YUV(width, height, rgbData);

            string dirc = @"D:\ScreenTest\";
            if (!Directory.Exists(dirc))
            {
                Directory.CreateDirectory(dirc);
            }

            string path = Path.Combine(dirc, frame + ".bmp");
            byte[] bmp = new byte[width * height * 3];
            ConvertYUV2RGB(result, bmp, width, height);

            //if (!File.Exists(path))
            //{
            //    File.Create(dirc);
            //}
            Debug.Log("YUVDownTest  path:" + path);
            WriteBMP(bmp, width, height, path);
        }

    }

    /// <summary>
    /// 将转换后的 RGB 图像数据按照 BMP 格式写入文件。
    /// </summary>
    /// <param name="rgbFrame">RGB 格式图像数据。</param>
    /// <param name="width">图像宽（单位：像素）。</param>
    /// <param name="height">图像高（单位：像素）。</param>
    /// <param name="bmpFile"> BMP 文件名。</param>
    public static void WriteBMP(byte[] rgbFrame, int width, int height, string bmpFile)
    {
        // 写 BMP 图像文件。
        int yu = width * 3 % 4;
        int bytePerLine = 0;
        yu = yu != 0 ? 4 - yu : yu;
        bytePerLine = width * 3 + yu;

        using (FileStream fs = File.Open(bmpFile, FileMode.Create))
        {
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write('B');
                bw.Write('M');
                bw.Write(bytePerLine * height + 54);
                bw.Write(0);
                bw.Write(54);
                bw.Write(40);
                bw.Write(width);
                bw.Write(height);
                bw.Write((ushort)1);
                bw.Write((ushort)24);
                bw.Write(0);
                bw.Write(bytePerLine * height);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                byte[] data = new byte[bytePerLine * height];
                int gIndex = width * height;
                int bIndex = gIndex * 2;

                for (int y = height - 1, j = 0; y >= 0; y--, j++)
                {
                    for (int x = 0, i = 0; x < width; x++)
                    {
                        data[y * bytePerLine + i++] = rgbFrame[bIndex + j * width + x];    // B
                        data[y * bytePerLine + i++] = rgbFrame[gIndex + j * width + x];    // G
                        data[y * bytePerLine + i++] = rgbFrame[j * width + x];  // R
                    }
                }

                bw.Write(data, 0, data.Length);
                bw.Flush();
            }
        }
    }

    /// <summary>
    /// 将一桢 YUV 格式的图像转换为一桢 RGB 格式图像。
    /// </summary>
    /// <param name="yuvFrame">YUV 格式图像数据。</param>
    /// <param name="rgbFrame">RGB 格式图像数据。</param>
    /// <param name="width">图像宽（单位：像素）。</param>
    /// <param name="height">图像高（单位：像素）。</param>
    public static void ConvertYUV2RGB(byte[] yuvFrame, byte[] rgbFrame, int width, int height)
    {
        int uIndex = width * height;
        int vIndex = uIndex + ((width * height) >> 2);
        int gIndex = width * height;
        int bIndex = gIndex * 2;

        int temp = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // R分量
                temp = (int)(yuvFrame[y * width + x] + (yuvFrame[vIndex + (y / 2) * (width / 2) + x / 2] - 128) * YUV2RGB_CONVERT_MATRIX[0, 2]);
                rgbFrame[y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));

                // G分量
                temp = (int)(yuvFrame[y * width + x] + (yuvFrame[uIndex + (y / 2) * (width / 2) + x / 2] - 128) * YUV2RGB_CONVERT_MATRIX[1, 1] + (yuvFrame[vIndex + (y / 2) * (width / 2) + x / 2] - 128) * YUV2RGB_CONVERT_MATRIX[1, 2]);
                rgbFrame[gIndex + y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));

                // B分量
                temp = (int)(yuvFrame[y * width + x] + (yuvFrame[uIndex + (y / 2) * (width / 2) + x / 2] - 128) * YUV2RGB_CONVERT_MATRIX[2, 1]);
                rgbFrame[bIndex + y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));
            }
        }
    }

    static byte[] RGB2YUV(int width, int height, byte[] rgbData)
    {

        byte r, g, b;
        byte[] yuvList = new byte[width * height * 3 / 2];
        int count = 0;
        int ycount = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                r = rgbData[4 * ((height - i - 1) * width + j) + 0];
                g = rgbData[4 * ((height - i - 1) * width + j) + 1];
                b = rgbData[4 * ((height - i - 1) * width + j) + 2];

                yuvList[ycount] = Convert.ToByte(((66 * r + 129 * g + 25 * b + 128) >> 8) + 16);
                ycount++;
                if (j % 2 == 0 && i % 2 == 0)
                {
                    yuvList[height * width + count] = Convert.ToByte(((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128);
                    int index = height * width * 5 / 4 + count;
                    if (yuvList.Length > index)
                    {
                        yuvList[height * width * 5 / 4 + count] = Convert.ToByte(((112 * r - 94 * g - 18 * b + 128) >> 8) + 128);
                    }

                    count++;
                }

            }
        }
        return yuvList;
    }
}
