using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class PresentationBudget:MonoBehaviour
    {
        sealed class Entry{public Transform owner,body;public Behaviour visual;public float range;public bool active,pending;}
        static readonly List<Entry> entries=new List<Entry>();readonly Queue<Entry> pending=new Queue<Entry>();float next;
        public static void Register(Behaviour visual,float range){visual.enabled=false;entries.Add(new Entry{owner=visual.transform,visual=visual,range=range});}
        public static void RegisterWildlife(Wildlife wildlife){var body=wildlife.transform.Find("Body");if(body)entries.Add(new Entry{owner=wildlife.transform,body=body,range=500,active=true});}
        void Update()
        {
            var camera=Camera.main;if(!camera)return;Vector3 view=camera.transform.position;float scope=Ranger.Local&&Ranger.Local.Aiming?1.6f:1;
            if(Time.unscaledTime>=next){next=Time.unscaledTime+.15f;for(int i=entries.Count-1;i>=0;i--){var entry=entries[i];if(!entry.owner){entries.RemoveAt(i);continue;}float range=entry.range*(.72f+RuntimePerformance.Detail*.28f)*(entry.range>=400?scope:1);bool active=(entry.owner.position-view).sqrMagnitude<range*range;if(active==entry.active)continue;if(active&&entry.visual){if(!entry.pending){entry.pending=true;pending.Enqueue(entry);}continue;}entry.active=active;if(entry.visual)entry.visual.enabled=active;if(entry.body)entry.body.gameObject.SetActive(active);}}
            for(int budget=0;budget<4&&pending.Count>0;budget++){var entry=pending.Dequeue();entry.pending=false;if(!entry.owner||!entry.visual||entry.active)continue;float range=entry.range*(.72f+RuntimePerformance.Detail*.28f)*(entry.range>=400?scope:1);if((entry.owner.position-view).sqrMagnitude>range*range)continue;entry.active=true;entry.visual.enabled=true;}
        }
        void OnDestroy(){entries.Clear();}
    }
}
