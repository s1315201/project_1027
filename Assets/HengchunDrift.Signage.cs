using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    readonly Dictionary<Font,Material> worldFontMaterials=new Dictionary<Font,Material>();
    bool fontAtlasHooked;
    Material WorldFontMaterial(Font source)
    {
        if(!fontAtlasHooked){Font.textureRebuilt+=RefreshWorldFont; fontAtlasHooked=true;}
        if(worldFontMaterials.TryGetValue(source,out var result))return result;
        var shader=Resources.Load<Shader>("NightMarketWorldText");
        result=new Material(shader?shader:source.material.shader);result.mainTexture=source.material.mainTexture;worldFontMaterials.Add(source,result);return result;
    }
    void RefreshWorldFont(Font source)
    {
        if(worldFontMaterials.TryGetValue(source,out var material)&&material)material.mainTexture=source.material.mainTexture;
    }
}
