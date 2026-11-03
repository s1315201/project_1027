using UnityEngine;

public partial class HengchunDrift
{
    void SetupVisitorGesture(Walker walker,int variant)
    {
        walker.gesture=variant%4;walker.head=walker.root.Find("Head");
        if(walker.gesture==1){
            walker.cameraProp=Box("Tourist compact camera",new Vector3(0,1.52f,.27f),new Vector3(.18f,.11f,.07f),dark,walker.root);
            var lens=Shape("Camera lens",PrimitiveType.Cylinder,new Vector3(0,1.52f,.32f),new Vector3(.065f,.04f,.065f),M("#71828b"),walker.root);lens.localRotation=Quaternion.Euler(90,0,0);lens.SetParent(walker.cameraProp,true);
        }
        if(variant%7==0){
            Shape("Tourist sun hat",PrimitiveType.Cylinder,new Vector3(0,1.92f,0),new Vector3(.45f,.035f,.45f),M("#d6bd83"),walker.root);
            Shape("Straw hat crown",PrimitiveType.Sphere,new Vector3(0,1.94f,0),new Vector3(.29f,.18f,.29f),M("#d6bd83"),walker.root);
        }
        if(variant%6==0)Box("Shopping tote",new Vector3(0,-.5f,.05f),new Vector3(.24f,.32f,.12f),M("#d9c4a0"),walker.leftArm);
    }
    void AnimateVisitors(float dt)
    {
        foreach(var w in walkers){
            if(!w.root)continue;
            bool photo=w.gesture==1&&(clock+w.phase)%12>8;
            bool wave=w.gesture==2&&(clock+w.phase)%14>10;
            bool walking=w.travel>0&&!photo&&!wave;
            if(walking)w.walkClock+=dt*w.pace;
            float t=w.walkClock+w.phase;float swing=walking?Mathf.Sin(t*5)*24:0;
            float bob=walking?Mathf.Abs(Mathf.Sin(t*5))*.017f:0;
            w.root.localPosition=w.origin+Vector3.forward*Mathf.Sin(t*.35f)*w.travel+Vector3.up*bob;
            w.leftLeg.localRotation=Quaternion.Euler(swing,0,0);w.rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            w.leftArm.localRotation=Quaternion.Euler(photo?-74:-swing*.7f,0,photo?-15:-7);
            w.rightArm.localRotation=Quaternion.Euler(photo?-74:swing*.7f,0,photo?15:wave?-145+Mathf.Sin(clock*9)*12:7);
            if(w.travel>0)w.root.localRotation=Quaternion.Euler(0,Mathf.Cos(t*.35f)>0?0:180,0);
            if(w.head)w.head.localRotation=Quaternion.Euler(0,Mathf.Sin(clock*.6f+w.phase)*(photo?4:18),0);
            if(w.cameraProp)w.cameraProp.gameObject.SetActive(photo);
        }
    }
}

