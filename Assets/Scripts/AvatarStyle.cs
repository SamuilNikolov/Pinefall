using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class AvatarStyle:MonoBehaviour
    {
        Ranger p;Transform original,alternate,gear,gun;int stamp=-1,held=-2,lastToolSwing;float toolSwingAt=-10;readonly List<Material> mats=new List<Material>();
        public static readonly Color[] colors={new Color(.30f,.40f,.27f),new Color(.56f,.45f,.30f),new Color(.78f,.84f,.86f),new Color(.22f,.29f,.33f),new Color(.54f,.29f,.21f),new Color(.38f,.34f,.48f)};
        public static Material PaletteMaterial(Material original,int palette)
        {var m=new Material(Shader.Find("Pinefall/RangerCloth"));m.mainTexture=original.mainTexture;m.SetColor("_Tint",colors[Mathf.Clamp(palette,0,5)]);if(original.HasProperty("_NormalTexture"))m.SetTexture("_BumpMap",original.GetTexture("_NormalTexture"));return m;}
        void Start(){p=GetComponent<Ranger>();original=p.body;}
        void LateUpdate()
        {
            if(!p)return;if(held!=p.Weapon.Value+p.ToolMode.Value*100){held=p.Weapon.Value+p.ToolMode.Value*100;RebuildHeldWeapon();}int value=p.Appearance.Value*100+p.Palette.Value*10+0;
            if(value!=stamp){stamp=value;Rebuild();}
            var cam=p.view?p.view.GetComponent<RangerCamera>():null;bool visible=p.Alive&&(!p.LocalHuman||(cam&&cam.ExternalView));
            if(original)original.gameObject.SetActive(visible&&p.Appearance.Value==0);
            if(alternate)alternate.gameObject.SetActive(visible&&p.Appearance.Value==1);
            if(gun)gun.gameObject.SetActive(visible&&(p.ToolMode.Value!=0||p.Weapon.Value>=0)&&(!p.CurrentVehicle||p.CurrentVehicle.Type.Value==4||p.VehicleSeat.Value>0));
            var selected=p.Appearance.Value==1&&alternate?alternate:original;if(selected)selected.localRotation=Quaternion.Slerp(selected.localRotation,p.Downed.Value?Quaternion.Euler(0,0,70):Quaternion.identity,Time.deltaTime*5);
            p.ApplyStanceVisual(selected);if(gun)gun.localPosition=new Vector3(.18f,p.Sliding.Value?.50f:p.Crouched.Value?.78f:1.12f,.32f);
            if(lastToolSwing!=p.ToolSwings.Value){lastToolSwing=p.ToolSwings.Value;toolSwingAt=Time.time;}if(gun&&p.ToolMode.Value==1)gun.localRotation=Quaternion.Euler(-Mathf.Sin(Mathf.Clamp01((Time.time-toolSwingAt)/.42f)*Mathf.PI)*65,0,0);
            p.ApplyWeaponPose(selected,gun);
            if(p.rig)p.rig.Pose(p.Velocity.Value,!p.Alive);
        }
        void Rebuild()
        {
            if(p.Appearance.Value==1&&!alternate){var asset=Resources.Load<GameObject>("AlternateAvatar");if(asset)alternate=Instantiate(asset,transform).transform;}
            Transform selected=p.Appearance.Value==1&&alternate?alternate:original;p.rig=selected?selected.GetComponentInChildren<VisualRig>():null;
            if(selected)foreach(var r in selected.GetComponentsInChildren<Renderer>(true)){var copy=PaletteMaterial(r.sharedMaterial,p.Palette.Value);r.sharedMaterial=copy;mats.Add(copy);}
            // Placeholder backpacks and primitive helmets were removed; use the actual outfit mesh.


            if(gun)foreach(var t in gun.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
        }
        void RebuildHeldWeapon()
        {
            if(gun)Destroy(gun.gameObject);if(p.ToolMode.Value>0){gun=new GameObject("Held field tool").transform;gun.SetParent(transform,false);Ranger.CreatePickaxe(gun,false);gun.localScale=Vector3.one*.7f;return;}if(p.Weapon.Value<0)return;if(p.Weapon.Value==7){gun=new GameObject("Held rocket launcher").transform;gun.SetParent(transform,false);LauncherModel.Create(gun);foreach(var part in gun.GetComponentsInChildren<Transform>())part.gameObject.layer=9;return;}var resource=Resources.Load<GameObject>(p.Weapon.Value==3||p.Weapon.Value==4?"RealPistol":"RealRifle");if(!resource)return;
            gun=Instantiate(resource,transform,false).transform;gun.localPosition=new Vector3(.18f,1.12f,.32f);gun.localScale=Vector3.one*.82f;gun.localRotation=Quaternion.Euler(10,0,0);foreach(var t in gun.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;
        }
        void OnDestroy(){foreach(var m in mats)if(m)Destroy(m);}
    }
}
