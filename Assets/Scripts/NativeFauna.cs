using UnityEngine;
namespace Pinefall
{
    // Smaller wildlife uses a compact animated rig; large animals retain the imported skeletal clips.
    public sealed class NativeFauna:MonoBehaviour
    {
        public Transform[] legs;public Transform torso;public bool hare;
        Wildlife animal;
        void Start(){animal=GetComponentInParent<Wildlife>();}
        void Update()
        {
            if(!animal||!torso)return;
            if(animal.Dead){torso.localRotation=Quaternion.Slerp(torso.localRotation,Quaternion.Euler(0,0,85),Time.deltaTime*4);return;}
            float speed=animal.MoveSpeed.Value,phase=Time.time*(speed>2?16:6)+animal.NetworkObjectId;
            torso.localPosition=new Vector3(0,speed>.1f?(hare?Mathf.Abs(Mathf.Sin(phase))*.13f:Mathf.Sin(phase*2)*.025f):0,0);
            for(int i=0;i<legs.Length;i++)if(legs[i])legs[i].localRotation=Quaternion.Euler(speed>.1f?Mathf.Sin(phase+(i%2)*Mathf.PI)*32:0,0,0);
        }
    }
}
