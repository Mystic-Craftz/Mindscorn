using System;
using System.IO;
using UnityEngine;

public class IconGenerator : MonoBehaviour
{
    //* To be used only on the IconGeneration scene
    //* Generates an image from the camera view
    //* And saves it in the screenshot folder
    private void Start()
    {
        // TakeScreenshot();
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = Color.black; // background ignored when ClearFlags=Skybox
            cam.cullingMask = ~0; // all layers
        }
        // optional: disable other cameras at runtime
        foreach (var other in FindObjectsOfType<Camera>())
        {
            if (other != cam) other.enabled = false;
        }

        string filename = string.Format("Assets/Screenshots/capture_{0}.png", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
        if (!Directory.Exists("Assets/Screenshots"))
        {
            Directory.CreateDirectory("Assets/Screenshots");
        }
        TakeTransparentScreenshot(cam, Screen.width, Screen.height, filename);
    }

    public void TakeTransparentScreenshot(Camera cam, int width, int height, string savePath)
    {
        var desc = new RenderTextureDescriptor(width, height);
        desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm; // explicit RGBA8
        desc.depthBufferBits = 24;
        var render_texture = RenderTexture.GetTemporary(desc);

        var bak_cam_targetTexture = cam.targetTexture;
        var bak_cam_clearFlags = cam.clearFlags;
        var bak_bgColor = cam.backgroundColor;
        var bak_allowHDR = cam.allowHDR;

        RenderTexture.active = render_texture;
        GL.Clear(true, true, new Color(0, 0, 0, 0)); // ensure fully transparent clear

        cam.targetTexture = render_texture;
        cam.clearFlags = CameraClearFlags.Nothing; // don't overwrite our clear
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.allowHDR = false;
        cam.forceIntoRenderTexture = true;
        cam.Render();

        var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex_transparent.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex_transparent.Apply();

        Color px = tex_transparent.GetPixel(width / 2, height / 2);
        // Debug.Log($"Pixel RGBA center: {px.r}, {px.g}, {px.b}, {px.a}");

        byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
        File.WriteAllBytes(savePath, pngShot);

        cam.clearFlags = bak_cam_clearFlags;
        cam.backgroundColor = bak_bgColor;
        cam.allowHDR = bak_allowHDR;
        cam.targetTexture = bak_cam_targetTexture;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(render_texture);
        Destroy(tex_transparent);
    }

    // public void TakeTransparentScreenshot(Camera cam, int width, int height, string savePath)
    // {
    //     // ScreenCapture.CaptureScreenshot("Assets/Screenshots/myscreen.png");
    //     // Depending on your render pipeline, this may not work.
    //     var bak_cam_targetTexture = cam.targetTexture;
    //     var bak_cam_clearFlags = cam.clearFlags;
    //     var bak_RenderTexture_active = RenderTexture.active;

    //     var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
    //     // Must use 24-bit depth buffer to be able to fill background.
    //     var render_texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf);
    //     render_texture.useMipMap = false;
    //     render_texture.autoGenerateMips = false;
    //     var grab_area = new Rect(0, 0, width, height);

    //     RenderTexture.active = render_texture;
    //     cam.targetTexture = render_texture;
    //     cam.clearFlags = CameraClearFlags.SolidColor;

    //     // Simple: use a clear background
    //     GL.Clear(true, true, new Color(0, 0, 0, 0));

    //     cam.targetTexture = render_texture;
    //     cam.clearFlags = CameraClearFlags.Nothing; // don't overwrite our transparent clear
    //     cam.backgroundColor = new Color(0, 0, 0, 0); // still safe
    //     cam.Render();

    //     tex_transparent.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    //     tex_transparent.Apply();

    //     // Encode the resulting output texture to a byte array then write to the file
    //     byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
    //     File.WriteAllBytes(savePath, pngShot);

    //     cam.clearFlags = bak_cam_clearFlags;
    //     cam.targetTexture = bak_cam_targetTexture;
    //     RenderTexture.active = bak_RenderTexture_active;
    //     RenderTexture.ReleaseTemporary(render_texture);
    //     var pixel = tex_transparent.GetPixel(width / 2, height / 2);
    //     Debug.Log($"Pixel RGBA: {pixel}");
    //     Texture2D.Destroy(tex_transparent);
    // }
}
