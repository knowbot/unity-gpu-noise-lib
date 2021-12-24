using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SaveShaderTexture : MonoBehaviour
{
    public int TextureLength = 1024;

    public Texture2D texture;
    public void Save()
    {

         RenderTexture buffer = new RenderTexture(
                               TextureLength, 
                               TextureLength, 
                               0,                            // No depth/stencil buffer
                               RenderTextureFormat.ARGB32,   // Standard colour format
                               RenderTextureReadWrite.sRGB // No sRGB conversions
                           );

        texture = new Texture2D(TextureLength,TextureLength,TextureFormat.ARGB32,true);

        MeshRenderer render = GetComponent<MeshRenderer>();
        //texture = render.sharedMaterial.GetTexture("_MainTex") as Texture2D;
        Material material = render.sharedMaterial;

        Graphics.Blit(null, buffer, material);
        RenderTexture.active = buffer;           // If not using a scene camera

        texture.ReadPixels(
          new Rect(0, 0, TextureLength, TextureLength), // Capture the whole texture
          0, 0,                          // Write starting at the top-left texel
          false);                          // No mipmaps

        System.IO.File.WriteAllBytes(Application.dataPath + "/"+"SkinLut.png", texture.EncodeToPNG()); 
        // texture.Save();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Save();
    }

    // Update is called once per frame
    void Update()
    {
        // Save();
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Save();
        }
    }
}