using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoStart : MonoBehaviour
{
    private Coroutine liveDataCoroutine;
    private ScreenFrameGenerator screenFrameGenerator;


    public Button beginCaptureScreenshot;
    public Button stopCaptureScreenshot;

    public Button dowloadCaptureScreenshot;

    public ComputeShader computeShader;

    // Start is called before the first frame update
    void Start()
    {
        screenFrameGenerator = new ScreenFrameGenerator();
        screenFrameGenerator.shader = computeShader;
        beginCaptureScreenshot.onClick.AddListener(OnClickBeginScreenshot);
        stopCaptureScreenshot.onClick.AddListener(OnClickStopScreenshot);
        dowloadCaptureScreenshot.onClick.AddListener(OnClickDownLoadScreenshot);
    }

    // Update is called once per frame
    private void OnClickBeginScreenshot() 
    {
        if (liveDataCoroutine != null)
        {
            StopCoroutine(liveDataCoroutine);
        }
        liveDataCoroutine = StartCoroutine(GetExternalLiveBuffer());
    }

    private void OnClickStopScreenshot()
    {
        if (liveDataCoroutine != null)
        {
            StopCoroutine(liveDataCoroutine);
        }
        liveDataCoroutine = null;
    }

    private void OnClickDownLoadScreenshot()
    {
        if (liveDataCoroutine == null)
        {
            return;
        }

        FormatConversionUtil.FrameActive = true;
        FormatConversionUtil.FrameByteActive = true;
        FormatConversionUtil.frame = 0;
    }

    IEnumerator GetExternalLiveBuffer()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (screenFrameGenerator == null)
            {
                liveDataCoroutine = null;
                yield break;
            }


            byte[] data = screenFrameGenerator.GetFrameBuffer();
        }
    }
}
