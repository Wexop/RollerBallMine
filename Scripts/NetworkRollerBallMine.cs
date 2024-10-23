using System.Linq;
using StaticNetcodeLib;
using Unity.Netcode;
using UnityEngine;

namespace RollerBallMine.Scripts;

[StaticNetcode]
public class NetworkRollerBallMine
{
    [ClientRpc]
    public static void DetectPlayerClientRpc(ulong networkId, Vector3 position)
    {
        var objects = Object.FindObjectsByType<RollerBallMine>(FindObjectsSortMode.None).ToList();
        var objectFound = objects.Find(e => e.NetworkObjectId == networkId);

        if (objectFound == null)
        {
            Debug.LogError($"ROLLER BALL NOT FOUND {networkId}");
        }
        else
        {
            objectFound.DetectPlayer(position);
        }
    }
    
    [ClientRpc]
    public static void ExplodeClientRpc(ulong networkId)
    {
        var objects = Object.FindObjectsByType<RollerBallMine>(FindObjectsSortMode.None).ToList();
        var objectFound = objects.Find(e => e.NetworkObjectId == networkId);

        if (objectFound == null)
        {
            Debug.LogError($"ROLLER BALL NOT FOUND {networkId}");
        }
        else
        {
            objectFound.Explode();
        }
    }
    
    [ClientRpc]
    public static void SetValueClientRpc(ulong networkId, int value)
    {
        var objects = Object.FindObjectsByType<RollerBallMine>(FindObjectsSortMode.None).ToList();
        var objectFound = objects.Find(e => e.NetworkObjectId == networkId);

        if (objectFound == null)
        {
            Debug.LogError($"ROLLER BALL NOT FOUND {networkId}");
        }
        else
        {
            objectFound.SetValue(value);
        }
    }

}