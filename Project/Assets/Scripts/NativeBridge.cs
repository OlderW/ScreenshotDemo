using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class NativeBridge
{
    // Start is called before the first frame update
    [DllImport("LiveAdapt")]
    public static extern void CopyUintToByte(uint[] uintData, int byteSize, byte[] byteData);
}
