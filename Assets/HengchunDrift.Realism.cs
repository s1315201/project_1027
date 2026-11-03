using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    Mesh humanHead,upperLimb,lowerLimb;
    readonly Dictionary<string,Material> realisticMaterials=new Dictionary<string,Material>();
    readonly Dictionary<string,Texture2D> surfaceDetails=new Dictionary<string,Texture2D>();
    Texture2D SurfaceDetail(string category)
    {
        if(surfaceDetails.TryGetValue(category,out var found))return found;
        const int size=128;var texture=new Texture2D(size,size);texture.name=category+" surface detail";texture.wrapMode=TextureWrapMode.Repeat;
        var pixels=new Color[size*size];var rng=new System.Random(618);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float noise=(float)rng.NextDouble(),v=1;
            if(category=="skin")v=.94f+noise*.06f+Mathf.PerlinNoise(x*.12f,y*.12f)*.025f;
            else if(category=="fabric")v=.83f+noise*.06f+((x%3==0||y%3==0)?0:.1f);
            else if(category=="hair")v=.7f+Mathf.PerlinNoise(x*.6f,y*.025f)*.3f;
            else if(category=="metal")v=.83f+noise*.08f+Mathf.PerlinNoise(x*.05f,y*1.4f)*.09f;
            else v=.87f+noise*.08f;
            pixels[y*size+x]=new Color(v,v,v);
        }
        texture.SetPixels(pixels);texture.Apply();surfaceDetails.Add(category,texture);return texture;
    }
    Material RealSurface(Material source,string category)
    {
        string key=source.GetEntityId()+":"+category;
        if(realisticMaterials.TryGetValue(key,out var material))return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Natural "+category;material.color=source.color;
        material.mainTexture=SurfaceDetail(category);material.mainTextureScale=category=="fabric"?new Vector2(3,3):Vector2.one;
        material.SetFloat("_Smoothness",category=="skin"?.27f:category=="metal"?.58f:category=="hair"?.25f:.12f);
        material.SetFloat("_Metallic",category=="metal"?.7f:0);
        if(category=="skin"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",source.color*.035f);}
        realisticMaterials.Add(key,material);return material;
    }
    Mesh SculptedHead()
    {
        if(humanHead)return humanHead;
        const int around=40,rows=36;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        float[] heights={0,.04f,.1f,.18f,.25f,.31f,.35f,.36f};
        float[] widths={.055f,.083f,.12f,.142f,.137f,.122f,.073f,.003f};
        float[] depths={.07f,.085f,.105f,.118f,.118f,.105f,.06f,.003f};
        for(int row=0;row<=rows;row++){
            float y=row*.36f/rows;int ring=0;while(ring<heights.Length-2&&y>heights[ring+1])ring++;
            float t=Mathf.InverseLerp(heights[ring],heights[ring+1],y),rx=Mathf.Lerp(widths[ring],widths[ring+1],t),rz=Mathf.Lerp(depths[ring],depths[ring+1],t);
            for(int i=0;i<=around;i++){
                float a=i*Mathf.PI*2/around,x=Mathf.Cos(a)*rx,z=Mathf.Sin(a)*rz,front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a)),12);
                float nose=Mathf.Exp(-Mathf.Pow(x/.027f,2)-Mathf.Pow((y-.16f)/.043f,2))*.049f;
                float brow=Mathf.Exp(-Mathf.Pow((y-.245f)/.017f,2))*.008f;
                float sockets=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.061f)/.032f,2)-Mathf.Pow((y-.21f)/.02f,2))*.009f;
                vertices.Add(new Vector3(x,y,z+front*(nose+brow-sockets)));uv.Add(new Vector2(i/(float)around,row/(float)rows));
                if(row>0&&i>0){int n=vertices.Count-1;triangles.Add(n-around-2);triangles.Add(n-1);triangles.Add(n-around-1);triangles.Add(n-around-1);triangles.Add(n-1);triangles.Add(n);}
            }
        }
        humanHead=new Mesh();humanHead.name="Sculpted human jaw cheeks brow and nose";humanHead.SetVertices(vertices);humanHead.SetUVs(0,uv);humanHead.SetTriangles(triangles,0);humanHead.RecalculateNormals();return humanHead;
    }
    Mesh TaperedLimb(bool upper)
    {
        if(upper&&upperLimb)return upperLimb;if(!upper&&lowerLimb)return lowerLimb;
        const int rings=12,sides=16;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++){
            float t=j/(float)rings,a=i*Mathf.PI*2/sides;
            float radius=Mathf.Sin(Mathf.PI*Mathf.Pow(t,.8f))*.5f*Mathf.Lerp(upper?.9f:.6f,1,t);
            v.Add(new Vector3(Mathf.Cos(a)*radius,-1+2*t,Mathf.Sin(a)*radius*.88f));uv.Add(new Vector2(i/(float)sides,t));
            if(j>0&&i>0){int n=v.Count-1;tris.Add(n-sides-2);tris.Add(n-1);tris.Add(n-sides-1);tris.Add(n-sides-1);tris.Add(n-1);tris.Add(n);}
        }
        var mesh=new Mesh();mesh.name="Anatomical tapered limb";mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();if(upper)upperLimb=mesh;else lowerLimb=mesh;return mesh;
    }
    void RealHumanFace(Transform root,bool rider,bool girl)
    {
        var parts=root.GetComponentsInChildren<Transform>();Transform head=null;Material tone=skin;
        foreach(var part in parts){
            if(part.name=="Head"||part.name=="Natural jaw and face"){head=part;tone=part.GetComponent<Renderer>().sharedMaterial;part.GetComponent<MeshFilter>().sharedMesh=SculptedHead();}
            string n=part.name;
            if(n=="Short fringe"||n=="Tousled black fringe"){
                part.GetComponent<MeshFilter>().sharedMesh=TaperedLimb(false);
                part.localScale=new Vector3(part.localScale.x*.6f,part.localScale.y,part.localScale.z*.55f);
            }
            if(n=="Layered chestnut hair"){
                part.GetComponent<MeshFilter>().sharedMesh=TaperedLimb(false);
                float variation=Mathf.Sin(part.localPosition.x*45)*.025f;
                part.localPosition+=Vector3.up*variation;
            }
            if(n=="Eye"||n.StartsWith("Comic eye")||n.StartsWith("Comic brown")||n=="Eye white"||n=="Brown iris"||n=="Pupil"||n=="Eye highlight"||n=="Eyebrow"||n=="Ink eyebrow"||n=="Visitor nose"||n=="Nose tip"||n=="Nose bridge"||n=="Upper lip"||n=="Lower lip"||n=="Cheek"||n=="Small comic smile")part.gameObject.SetActive(false);
            if(n=="Upper arm"||n=="Forearm"||n=="Calf"||n=="Bare calf"||n=="Arm")part.GetComponent<MeshFilter>().sharedMesh=TaperedLimb(n=="Upper arm"||n=="Arm");
        }
        if(!head)return;
        for(int side=-1;side<=1;side+=2){
            Shape("Natural sclera",PrimitiveType.Sphere,new Vector3(side*.059f,.212f,.128f),new Vector3(.045f,.019f,.011f),M("#e9dfd4"),head);
            Shape("Natural iris",PrimitiveType.Sphere,new Vector3(side*.059f,.212f,.135f),new Vector3(.016f,.017f,.006f),M("#49382e"),head);
            Shape("Natural pupil",PrimitiveType.Sphere,new Vector3(side*.059f,.212f,.139f),new Vector3(.007f,.011f,.003f),M("#211e1c"),head);
            Limb("Upper eyelid",new Vector3(side*.039f,.22f,.13f),new Vector3(side*.079f,.218f,.128f),.0035f,tone,head);
            Limb("Natural brow",new Vector3(side*.034f,.244f,.125f),new Vector3(side*.087f,.242f,.117f),.0045f,M("#49392d"),head);
            Shape("Nostril shadow",PrimitiveType.Sphere,new Vector3(side*.019f,.145f,.154f),new Vector3(.012f,.005f,.005f),M("#835e48"),head);
            if(!rider)Shape("Natural ear",PrimitiveType.Sphere,new Vector3(side*.143f,.18f,-.005f),new Vector3(.035f,.075f,.034f),tone,head);
        }
        Limb("Natural mouth line",new Vector3(-.03f,.105f,.119f),new Vector3(.03f,.105f,.119f),.0028f,M(girl?"#9f6960":"#916454"),head);
        Shape("Subtle lower lip",PrimitiveType.Sphere,new Vector3(0,.1f,.121f),new Vector3(.048f,.008f,.009f),M(girl?"#b67e72":"#b1816d"),head);
        ApplyNaturalMaterials(root);
        ApplyFaceAtlas(head,girl);
    }
    void ApplyNaturalMaterials(Transform root)
    {
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
            if(r.GetComponent<TextMesh>())continue;
            if(r.sharedMaterial.name.StartsWith("Natural "))continue;
            string n=r.name.ToLowerInvariant();
            string category=n.Contains("hair")||n.Contains("fringe")||n.Contains("ponytail")?"hair":n.Contains("chain")||n.Contains("pendant")||n.Contains("earring")?"metal":n.Contains("face")||n=="head"||n.Contains("ear")||n.Contains("calf")||n.Contains("arm")||n.Contains("hand")||n.Contains("finger")||n.Contains("foot")||n.Contains("toe")||n=="neck"||n.Contains("eyelid")?"skin":n.Contains("tank")||n.Contains("short")||n.Contains("shirt")||n.Contains("blouse")||n.Contains("seam")?"fabric":"matte";
            r.sharedMaterial=RealSurface(r.sharedMaterial,category);
        }
    }
}

