using UnityEngine;

namespace Pinefall
{
    public sealed class VisualRig : MonoBehaviour
    {
        public AnimationClip idle,walk,run,death,attack;
        Animation anim;string current="";
        void Awake()
        {
            foreach(var a in GetComponentsInChildren<Animator>()) a.enabled=false;
            anim=GetComponent<Animation>();if(!anim)anim=gameObject.AddComponent<Animation>();
            Add(idle,"Idle",true);Add(walk,"Walk",true);Add(run,"Run",true);Add(death,"Death",false);
            Add(attack,"Attack",true);
            anim.cullingType=AnimationCullingType.BasedOnRenderers;
        }
        void Add(AnimationClip clip,string name,bool loop){if(!clip)return;anim.AddClip(clip,name);anim[name].wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;}
        public void Pose(float speed,bool dead=false,bool attacking=false)
        {
            string next=dead&&death?"Death":attacking&&attack?"Attack":speed>2.4f&&run?"Run":speed>.18f&&walk?"Walk":"Idle";
            if(!anim[next]||next==current&&anim.IsPlaying(next))return;current=next;anim.CrossFade(next,.22f);
        }
    }
}
