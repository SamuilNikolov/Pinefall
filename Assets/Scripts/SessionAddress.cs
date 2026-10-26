using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
namespace Pinefall
{
    public sealed class SessionAddress:MonoBehaviour
    {
        public static string PublicIP="Checking internet address…";
        IEnumerator Start()
        {
            using(var request=UnityWebRequest.Get("https://api.ipify.org"))
            {request.timeout=7;yield return request.SendWebRequest();IPAddress ip;PublicIP=request.result==UnityWebRequest.Result.Success&&IPAddress.TryParse(request.downloadHandler.text.Trim(),out ip)?ip.ToString():"Internet address unavailable";}
        }
    }
}
