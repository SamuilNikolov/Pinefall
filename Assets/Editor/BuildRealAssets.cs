using System.Linq;
using UnityEditor;
using UnityEngine;
public static partial class BuildReserve
{
    static void CreateRealAssets()
    {
        DetailedModel("RealRifle","bolt_action_rifle_7_62",1.10f,true);
        DetailedModel("RealPistol","service_pistol",.255f,true);
        DetailedModel("RealFern","fern_02",.65f,false);
    }
    static void DetailedModel(string resource,string ident,float size,bool weapon)
    {
        var root=new GameObject(resource);var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RealModels/"+ident+"/"+ident+".gltf");
        if(!source)throw new System.Exception("Detailed model missing: "+ident);var model=Object.Instantiate(source,root.transform);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
        {
            string n=renderer.gameObject.name;bool remove=ident=="service_pistol"&&(n.EndsWith("_b")||n.Contains("magazine")||n.Contains("bullet"))||ident=="bolt_action_rifle_7_62"&&(n.EndsWith("_b")||n.Contains("bullet"))||ident=="fern_02"&&n!="fern_02_a";
            if(remove){renderer.gameObject.SetActive(false);continue;}foreach(var mat in renderer.sharedMaterials){mat.enableInstancing=true;EditorUtility.SetDirty(mat);}
        }
        if(weapon)model.transform.localRotation=Quaternion.Euler(0,90,0);
        var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        float scale=size/(weapon?bounds.size.z:bounds.size.y);model.transform.localScale*=scale;Vector3 pivot=weapon?bounds.center:new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);model.transform.localPosition-=pivot*scale;
        Debug.Log("DETAILED_MODEL "+resource+" "+bounds+" scale "+scale);
        PrefabUtility.SaveAsPrefabAsset(root,R+resource+".prefab");Object.DestroyImmediate(root);
    }
}
