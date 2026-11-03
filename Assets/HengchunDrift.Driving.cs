using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    float rideHeading,steerHoldSeconds;
    int heldDriftSide;
    Vector3 lastTyreContact;
    bool tyreWasMarking;
    readonly Queue<Transform> tyreMarks=new Queue<Transform>();
    float SteeringInput => (rightLeanHeld||Held(UnityEngine.InputSystem.Key.D)||Held(UnityEngine.InputSystem.Key.RightArrow)?1:0)-(leftLeanHeld||Held(UnityEngine.InputSystem.Key.A)||Held(UnityEngine.InputSystem.Key.LeftArrow)?1:0);
    void ResetManualDrive()
    {
        rideHeading=0;steerHoldSeconds=0;heldDriftSide=0;tyreWasMarking=false;tyreMarks.Clear();
    }
    bool MoveManualDrive(float dt)
    {
        UpdateLateralSteering(dt);
        Vector3 target=scooter.position+Quaternion.Euler(0,rideHeading,0)*Vector3.forward*speed*dt;
        if(!IsOnDeliveryRoad(target)||!MoveScooterSafely(target))return false;
        // Measure progress from the actual vehicle; the route never teleports or steers it.
        float nearest=float.MaxValue,progress=distance;
        int start=Mathf.Max(0,Mathf.FloorToInt(distance)-20),end=Mathf.Min(course.Count-2,Mathf.CeilToInt(distance+speed*dt)+35);
        for(int i=start;i<=end;i++){
            Vector3 segment=course[i+1]-course[i];
            float t=Mathf.Clamp01(Vector3.Dot(target-course[i],segment)/segment.sqrMagnitude);
            float error=(target-(course[i]+segment*t)).sqrMagnitude;
            if(error<nearest){nearest=error;progress=i+t;}
        }
        distance=Mathf.Min(courseLength,Mathf.Max(distance,progress));return true;
    }
    void UpdateTyreMarks(float dt)
    {
        int side=(int)SteeringInput;
        if(side==0||side!=heldDriftSide)steerHoldSeconds=0;
        if(side!=0)steerHoldSeconds+=dt;
        heldDriftSide=side;
        bool braking=brakeTouch||Held(UnityEngine.InputSystem.Key.S)||Held(UnityEngine.InputSystem.Key.DownArrow);
        bool marking=speed>2&&(braking||(distance>=800&&side!=0&&steerHoldSeconds>=.45f));
        Vector3 contact=scooter.TransformPoint(new Vector3(0,0,-.72f));contact.y=.081f;
        if(!marking){tyreWasMarking=false;return;}
        if(!tyreWasMarking){lastTyreContact=contact;tyreWasMarking=true;effects.PlayOneShot(skidClip,.25f);return;}
        Vector3 delta=contact-lastTyreContact;float length=delta.magnitude;
        if(length<.12f)return;
        if(length>3){lastTyreContact=contact;return;}
        Transform mark;
        if(tyreMarks.Count>=320)mark=tyreMarks.Dequeue();
        else mark=Box("Persistent tyre skid mark",Vector3.zero,Vector3.one,M("#161c20"),courseRoot);
        mark.position=(contact+lastTyreContact)*.5f;
        mark.rotation=Quaternion.LookRotation(delta,Vector3.up);
        mark.localScale=new Vector3(.12f,.008f,length+.025f);
        tyreMarks.Enqueue(mark);lastTyreContact=contact;
    }
    void BuildDoubleYellowLines()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        foreach(float centre in new[]{-.16f,.16f}){
            int first=vertices.Count;
            for(int d=0;d<=1000;d+=2){
                var p=CoursePosition(d)+Vector3.up*.077f;var right=Quaternion.Euler(0,CourseHeading(d),0)*Vector3.right;
                vertices.Add(p+right*(centre-.045f));vertices.Add(p+right*(centre+.045f));
                int n=vertices.Count;
                if(n-first>2){triangles.Add(n-4);triangles.Add(n-2);triangles.Add(n-3);triangles.Add(n-3);triangles.Add(n-2);triangles.Add(n-1);}
            }
        }
        var mesh=new Mesh();mesh.name="Continuous double yellow road lines";mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
        var root=new GameObject("夜市雙黃線");root.transform.SetParent(courseRoot,false);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=M("#efc64d");
    }
    void BuildFabricObstacles()
    {
        for(int d=34,index=0;d<780;d+=62,index++){
            var group=new GameObject("布幔廣告障礙 · 請繞行").transform;group.SetParent(courseRoot,false);
            group.localPosition=CoursePosition(d);group.localRotation=Quaternion.Euler(0,CourseHeading(d),0);
            float side=index%2==0?-1:1;
            var cloth=new GameObject("Hanging cloth advertisement").transform;cloth.SetParent(group,false);cloth.localPosition=new Vector3(side*3.65f,0,0);
            var v=new List<Vector3>();var tri=new List<int>();
            for(int i=0;i<=12;i++){
                float x=-1+i/6f,sag=Mathf.Sin(i/12f*Mathf.PI)*.12f,z=Mathf.Sin(i*.95f)*.065f;
                v.Add(new Vector3(x,.48f-sag,z));v.Add(new Vector3(x,2.5f-sag,z));
                if(i>0){int n=v.Count;tri.Add(n-4);tri.Add(n-2);tri.Add(n-3);tri.Add(n-3);tri.Add(n-2);tri.Add(n-1);tri.Add(n-3);tri.Add(n-2);tri.Add(n-4);tri.Add(n-1);tri.Add(n-2);tri.Add(n-3);}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();cloth.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;cloth.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(index%2==0?"#c45b43":"#3d827b");
            var hit=cloth.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.45f,0);hit.size=new Vector3(2,2.2f,.18f);
            for(int edge=-1;edge<=1;edge+=2){
                Beam("Fabric advertisement frame",new Vector3(side*3.65f+edge,0,0),new Vector3(side*3.65f+edge,2.65f,0),.045f,dark,group);
                Box("Fabric stand weighted foot",new Vector3(side*3.65f+edge,.06f,0),new Vector3(.35f,.12f,.5f),dark,group);
            }
            environmentRoot=cloth;
            Label(index%2==0?"現炸\n鹽酥雞":"招牌\n烤魷魚",new Vector3(0,1.6f,-.09f),.095f,new Color(1,.94f,.7f),Quaternion.identity);
            Label("排隊這邊 →",new Vector3(0,.7f,-.09f),.038f,Color.white,Quaternion.identity);
        }
        environmentRoot=courseRoot;
    }
}
