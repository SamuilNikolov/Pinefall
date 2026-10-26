using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using Pinefall;
public static partial class BuildReserve
{
    static void CreateShark()
    {
        var root=new GameObject("Shark");root.layer=8;root.AddComponent<NetworkObject>();var nt=root.AddComponent<NetworkTransform>();nt.SyncScaleX=nt.SyncScaleY=nt.SyncScaleZ=false;var animal=root.AddComponent<Wildlife>();animal.species="Shark";animal.kind=2;animal.maxHealth=150;var hit=root.AddComponent<CapsuleCollider>();hit.direction=2;hit.radius=.5f;hit.height=3.6f;var body=new GameObject("Body").transform;body.SetParent(root.transform,false);var skin=MaterialAsset("Shark skin",new Color(.26f,.34f,.38f));var belly=MaterialAsset("Shark belly",new Color(.65f,.72f,.70f));
        Part("Streamlined body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.9f,.75f,3.3f),skin,body);Part("Pale underside",PrimitiveType.Sphere,new Vector3(0,-.19f,.2f),new Vector3(.79f,.48f,2.5f),belly,body);Part("Snout",PrimitiveType.Sphere,new Vector3(0,0,1.25f),new Vector3(.69f,.43f,.9f),skin,body);
        var mesh=new Mesh{name="Shark fins"};mesh.vertices=new[]{new Vector3(0,.27f,.05f),new Vector3(0,1.3f,-.35f),new Vector3(0,.27f,-.75f),new Vector3(-.3f,-.07f,.35f),new Vector3(-1.15f,-.18f,-.35f),new Vector3(-.28f,-.1f,-.6f),new Vector3(.3f,-.07f,.35f),new Vector3(1.15f,-.18f,-.35f),new Vector3(.28f,-.1f,-.6f),new Vector3(0,0,-1.3f),new Vector3(0,.75f,-2.1f),new Vector3(0,0,-1.85f),new Vector3(0,-.58f,-2),new Vector3(0,0,-1.3f)};mesh.triangles=new[]{0,1,2,2,1,0,3,4,5,5,4,3,6,7,8,8,7,6,9,10,11,11,10,9,11,12,13,13,12,11};mesh.RecalculateNormals();Save(mesh,"Assets/Generated/SharkFins.asset");var fins=new GameObject("Fins");fins.transform.SetParent(body,false);fins.AddComponent<MeshFilter>().sharedMesh=mesh;fins.AddComponent<MeshRenderer>().sharedMaterial=skin;
        var eyes=MaterialAsset("Shark eyes",new Color(.025f,.035f,.04f));foreach(int side in new[]{-1,1})Part("Eye",PrimitiveType.Sphere,new Vector3(side*.31f,.08f,1.28f),Vector3.one*.08f,eyes,body);foreach(var collider in body.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);SetLayer(body.gameObject,8);PrefabUtility.SaveAsPrefabAsset(root,R+"Shark.prefab");Object.DestroyImmediate(root);
    }
}
