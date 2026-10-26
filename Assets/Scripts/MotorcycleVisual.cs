using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class MotorcycleVisual:MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();readonly List<Transform> wheels=new List<Transform>();RoadVehicle bike;Transform body,front;
        Material Mat(string name,Color color,float metal=0){var m=WeaponView.Mat(name,color,metal);materials.Add(m);return m;}
        public void Build(Transform root)
        {
            bike=GetComponent<RoadVehicle>();body=root;var paint=Mat("Trail bike tank",new Color(.32f,.40f,.27f),.35f);var iron=Mat("Bike frame",new Color(.16f,.17f,.16f),.65f);var rubber=Mat("Bike tyres",new Color(.045f,.05f,.045f));
            WeaponView.Part(root,PrimitiveType.Capsule,new Vector3(0,.53f,.25f),new Vector3(.48f,.58f,.90f),paint,new Vector3(90,0,0));WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,.65f,-.40f),new Vector3(.39f,.16f,.85f),rubber);WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,.14f,-.02f),new Vector3(.29f,.38f,.65f),iron);WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(.29f,.23f,-.55f),new Vector3(.12f,.50f,.12f),iron,new Vector3(90,0,0));
            front=new GameObject("Steering fork").transform;front.SetParent(root,false);front.localPosition=new Vector3(0,.45f,.76f);
            foreach(float side in new[]{-.16f,.16f}){WeaponView.Part(front,PrimitiveType.Cylinder,new Vector3(side,-.30f,.12f),new Vector3(.045f,.42f,.045f),iron,new Vector3(13,0,0));WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(side,.2f,-.48f),new Vector3(.035f,.5f,.035f),iron,new Vector3(-48,0,0));}
            WeaponView.Part(front,PrimitiveType.Cylinder,new Vector3(0,.37f,-.10f),new Vector3(.055f,.42f,.055f),iron,new Vector3(0,0,90));var lamp=Mat("Bike headlamp",new Color(.95f,.88f,.65f));WeaponView.Part(front,PrimitiveType.Sphere,new Vector3(0,.10f,.22f),new Vector3(.23f,.23f,.12f),lamp);
            for(int i=0;i<2;i++){var wheel=new GameObject("Spoked tyre").transform;wheel.SetParent(i==0?front:root,false);wheel.localPosition=i==0?new Vector3(0,-.69f,.18f):new Vector3(0,-.24f,-.90f);wheel.localRotation=Quaternion.Euler(0,0,90);wheel.gameObject.AddComponent<MeshFilter>().sharedMesh=SoftGeometry.Tyre;wheel.gameObject.AddComponent<MeshRenderer>().sharedMaterial=rubber;wheel.localScale=new Vector3(.92f,.85f,.92f);wheels.Add(wheel);for(int spoke=0;spoke<6;spoke++)WeaponView.Part(wheel,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.025f,.35f,.025f),iron,new Vector3(0,spoke*30,90));}
            var collider=GetComponent<BoxCollider>();collider.size=new Vector3(.70f,1.35f,2.5f);collider.center=new Vector3(0,.25f,0);
        }
        void Update(){if(!bike)return;foreach(var wheel in wheels)wheel.Rotate(Vector3.up,bike.Speed.Value*Time.deltaTime*145,Space.Self);if(front)front.localRotation=Quaternion.Euler(0,bike.Steering.Value,0);if(body)body.localRotation=Quaternion.Euler(0,0,-bike.Steering.Value*Mathf.Clamp01(Mathf.Abs(bike.Speed.Value)/12)*.38f);}
        void OnDestroy(){foreach(var material in materials)if(material)Destroy(material);}
    }
}
