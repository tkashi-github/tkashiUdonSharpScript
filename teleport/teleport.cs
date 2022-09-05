using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TeleportByInterract : UdonSharpBehaviour
{
    [SerializeField] private Transform DestTransform;
    public override void Interact()
    {
        var player = Networking.LocalPlayer;

        player.TeleportTo(DestTransform.position, DestTransform.rotation);
    }
}
