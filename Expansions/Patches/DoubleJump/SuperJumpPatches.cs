using AK;
using HarmonyLib;
using UnityEngine;
using XpExpansions.Manager;

namespace Expansions.Patches.DoubleJump
{
    [HarmonyPatch]
    internal static class SuperJumpPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.Enter))]
        public static void Enter(PLOC_Jump __instance)
        {
            var player = __instance.m_owner;
            if (DoubleJumpManager.DoubleJumpUnlocked && InputMapper.GetButtonKeyMouse(InputAction.Crouch, player.InputFilter))
            {
                player.Sound.Post(EVENTS.IMPLANTSMALLJUMPBOOSTTRIGGER);
                player.Locomotion.VerticalVelocity += Vector3.up * DoubleJumpManager.ActiveConfig.SuperJumpVelocity;
            }
        }
    }
}
