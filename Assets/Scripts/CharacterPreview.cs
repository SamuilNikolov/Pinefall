using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class CharacterPreview:MonoBehaviour
    {
        public RenderTexture Image;public float Yaw=155;public bool Walking;
        GameObject stage,model;Camera previewCamera;VisualRig rig;int selection=-1;readonly List<Material> materials=new List<Material>();
        public void Show(int body,int palette)
        {
            if(!stage)
            {
                stage=new GameObject("Character dressing room");stage.transform.position=new Vector3(10000,10000,10000);
                Image=new RenderTexture(640,720,24,RenderTextureFormat.ARGB32);Image.Create();
                var go=new GameObject("Outfit preview previewCamera");go.transform.SetParent(stage.transform,false);previewCamera=go.AddComponent<Camera>();previewCamera.enabled=false;previewCamera.cullingMask=1<<11;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.105f,.14f,.15f);previewCamera.fieldOfView=33;previewCamera.nearClipPlane=.05f;previewCamera.farClipPlane=12;previewCamera.targetTexture=Image;
                go.transform.localPosition=new Vector3(0,1.04f,-3.8f);go.transform.LookAt(stage.transform.position+Vector3.up*.96f);
                var lamp=new GameObject("Dressing room light");lamp.transform.SetParent(stage.transform,false);lamp.transform.localRotation=Quaternion.Euler(35,-25,0);var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.5f;light.color=new Color(1,.94f,.85f);light.cullingMask=1<<11;
            }
            stage.SetActive(true);int value=body*10+palette;
            if(selection!=value)
            {
                selection=value;if(model)Destroy(model);foreach(var m in materials)if(m)Destroy(m);materials.Clear();
                var source=body==0?Resources.Load<GameObject>("Ranger").transform.Find("Body").gameObject:Resources.Load<GameObject>("AlternateAvatar");model=Instantiate(source,stage.transform,false);model.SetActive(true);
                foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=11;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()){var m=AvatarStyle.PaletteMaterial(renderer.sharedMaterial,palette);renderer.sharedMaterial=m;materials.Add(m);}
                rig=model.GetComponent<VisualRig>();
            }
            model.transform.localRotation=Quaternion.Euler(0,Yaw,0);if(rig)rig.Pose(Walking?3:0);previewCamera.Render();
        }
        public void Hide(){if(stage)stage.SetActive(false);}
        void OnDestroy(){if(stage)Destroy(stage);foreach(var m in materials)if(m)Destroy(m);if(Image){Image.Release();Destroy(Image);}}
    }
}
