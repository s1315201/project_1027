using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    Mesh maleTexturedHead,femaleTexturedHead;
    Material faceAlbedo;
    void ApplyFaceAtlas(Transform head,bool female)
    {
        if(!faceAlbedo){
            var texture=Resources.Load<Texture2D>("CharacterFaceAtlas");if(!texture)return;
            faceAlbedo=new Material(Shader.Find("Universal Render Pipeline/Lit"));faceAlbedo.name="Natural face atlas";
            faceAlbedo.mainTexture=texture;faceAlbedo.color=Color.white;faceAlbedo.SetFloat("_Smoothness",.25f);faceAlbedo.EnableKeyword("_EMISSION");faceAlbedo.SetColor("_EmissionColor",new Color(.025f,.025f,.025f));
        }
        Mesh projected=female?femaleTexturedHead:maleTexturedHead;
        if(!projected){
            var source=SculptedHead();projected=Instantiate(source);projected.name=female?"Female textured anatomical head":"Male textured anatomical head";
            var vertices=projected.vertices;var uv=new Vector2[vertices.Length];
            float[] ys={0,.105f,.16f,.212f,.245f,.36f};float[] vs={.052f,.26f,.365f,.522f,.59f,.99f};
            for(int i=0;i<vertices.Length;i++){
                var p=vertices[i];int band=0;while(band<ys.Length-2&&p.y>ys[band+1])band++;
                uv[i]=new Vector2((female?.753f:.264f)+p.x/.142f*.132f,Mathf.Lerp(vs[band],vs[band+1],Mathf.InverseLerp(ys[band],ys[band+1],p.y)));
            }
            var front=new List<int>();var back=new List<int>();var tris=source.triangles;
            for(int i=0;i<tris.Length;i+=3){
                bool facing=(vertices[tris[i]].z+vertices[tris[i+1]].z+vertices[tris[i+2]].z)>0;
                var target=facing?front:back;target.Add(tris[i]);target.Add(tris[i+1]);target.Add(tris[i+2]);
            }
            projected.uv=uv;projected.subMeshCount=2;projected.SetTriangles(back,0);projected.SetTriangles(front,1);
            if(female)femaleTexturedHead=projected;else maleTexturedHead=projected;
        }
        var renderer=head.GetComponent<MeshRenderer>();var skinMaterial=renderer.sharedMaterial;
        head.GetComponent<MeshFilter>().sharedMesh=projected;renderer.sharedMaterials=new[]{skinMaterial,faceAlbedo};
        // The albedo supplies subtle lips and lashes without cartoon outlines.
        foreach(var t in head.GetComponentsInChildren<Transform>())if(t.name=="Natural mouth line"||t.name=="Subtle lower lip"||t.name=="Natural brow")t.gameObject.SetActive(false);
    }
}
