using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
namespace Pinefall
{
    // Initial values may be assigned before Spawn; bind their containers before marking them dirty.
    public static class NetworkSeed
    {
        static readonly Dictionary<System.Type,FieldInfo[]> fields=new Dictionary<System.Type,FieldInfo[]>();
        public static void Initialize(NetworkBehaviour behaviour){var type=behaviour.GetType();if(!fields.TryGetValue(type,out var list)){list=type.GetFields(BindingFlags.Public|BindingFlags.Instance);fields[type]=list;}foreach(var field in list)if(field.GetValue(behaviour) is NetworkVariableBase variable)variable.Initialize(behaviour);}
    }
}
