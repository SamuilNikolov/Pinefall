using Unity.Netcode.Components;
namespace Pinefall
{
    public sealed class OwnerTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative(){return false;}
    }
}
