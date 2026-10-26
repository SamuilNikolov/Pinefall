using UnityEngine;
namespace Pinefall
{
    public sealed class PreviewAnimal : MonoBehaviour
    {
        VisualRig rig;
        void Start(){rig=GetComponent<VisualRig>();}
        void Update(){if(rig)rig.Pose(0);}
    }
}
