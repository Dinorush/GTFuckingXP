using AK;
using HarmonyLib;
using Player;
using XpExpansions.Manager;

namespace Expansions.Patches.DoubleJump
{
    [HarmonyPatch]
    internal static class PLOC_Patches
    {
        private static bool _canDoubleJump = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Enter))]
        [HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.Enter))]
        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Enter))]
        public static void Enter()
        {
            _canDoubleJump = DoubleJumpManager.DoubleJumpUnlocked;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PLOC_Fall), nameof(PLOC_Fall.Update))]
        public static void Update(PLOC_Fall __instance)
        {
            TryDoubleJump(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.Update))]
        public static void Update(PLOC_Jump __instance)
        {
            TryDoubleJump(__instance);
        }

        private static void TryDoubleJump(PLOC_Base state)
        {
            var player = state.m_owner;
            if (_canDoubleJump && InputMapper.GetButtonDown.Invoke(InputAction.Jump, player.InputFilter))
            {
                player.Sound.Post(EVENTS.FLAMERFIREBURST);
                state.UpdateHorizontalVelocityOnGround(player.PlayerData.runMoveSpeed);
                player.Locomotion.ChangeState(PlayerLocomotion.PLOC_State.Jump);
                _canDoubleJump = false;
            }
        }
    }
}
