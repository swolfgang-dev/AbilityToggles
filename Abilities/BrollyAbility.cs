using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class BrollyAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static ConfigEntry<bool> globalBindToQuickMapInput;
        private static SaveScopedConfigEntry<bool> saveEnabled;
        private static SaveScopedConfigEntry<bool> saveBindToQuickMapInput;
        private static bool quickMapConsumedForBrolly;
        private enum ExtraInputAction { QuickMap, Cast, QuickCast, SuperDash, DreamNail, None }
        private static ConfigEntry<ExtraInputAction> extraAction;
        private static ConfigEntry<UnityEngine.KeyCode> extraKey;
        private static ConfigEntry<InputControlType> extraController;

        private static readonly MethodInfo CanFloatMethod = AccessTools.Method(typeof(HeroController), "CanFloat");
        private static readonly MethodInfo StartFloatMethod = AccessTools.Method(typeof(HeroController), "StartFloat");
        private static readonly FieldInfo IsGameplaySceneField = AccessTools.Field(typeof(HeroController), "isGameplayScene");

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            extraAction = config.Bind("Controls", "Cloak Extra Game Action", ExtraInputAction.QuickMap,
                "Extra mapped game action for Drifter's Cloak. None uses only custom controls. The existing extra-input enable setting still applies.");
            extraKey = config.Bind("Controls", "Cloak Extra Keyboard Key", UnityEngine.KeyCode.None,
                "Optional extra cloak key. None leaves it disabled. Normal jump behavior remains available.");
            extraController = config.Bind("Controls", "Cloak Extra Controller Button", InputControlType.None,
                "Optional mapped controller control. None leaves it disabled. Avoid conflicts with game bindings.");
            globalEnabled = config.Bind(
                ConfigUi.GlobalOtherAbilities,
                "Drifters Cloak",
                false,
                ConfigUi.GlobalDescription("Allow Drifter's Cloak for every save without setting PlayerData.hasBrolly.", ConfigUi.DriftersCloakOrder, "Drifter's Cloak"));
            globalBindToQuickMapInput = config.Bind(
                ConfigUi.GlobalAdditionalOptions,
                "Bind Drifters Cloak to LB/L1",
                false,
                ConfigUi.GlobalDescription("Enable additional cloak controls configured in Controls for every save. Normal jump behavior remains available.", ConfigUi.BindDriftersCloakOrder, "Enable extra cloak controls"));
            saveEnabled = saveConfig.Bind(
                ConfigUi.OtherAbilities,
                "Drifters Cloak",
                false,
                ConfigUi.SaveDescription("Allow Drifter's Cloak without setting PlayerData.hasBrolly.", ConfigUi.DriftersCloakOrder, "Drifter's Cloak"));
            saveBindToQuickMapInput = saveConfig.Bind(
                ConfigUi.AdditionalOptions,
                "Bind Drifters Cloak to LB/L1",
                true,
                ConfigUi.SaveDescription("Enable additional cloak controls configured in Controls for this save. Normal jump behavior remains available.", ConfigUi.BindDriftersCloakOrder, "Enable extra cloak controls"));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool UsesQuickMapInput()
        {
            return (globalBindToQuickMapInput != null && globalBindToQuickMapInput.Value) ||
                (saveBindToQuickMapInput != null && saveBindToQuickMapInput.Value);
        }

        private static bool HasBrollyAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasBrolly) || IsEnabled();
        }

        private static IEnumerable<CodeInstruction> ReplaceHasBrollyChecks(IEnumerable<CodeInstruction> instructions)
        {
            var hasBrollyField = AccessTools.Field(typeof(PlayerData), "hasBrolly");
            var replacement = AccessTools.Method(typeof(BrollyAbility), nameof(HasBrollyAbility));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasBrollyField))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        private static bool CanFloat(HeroController heroController, bool checkControlState)
        {
            if (heroController == null || CanFloatMethod == null)
            {
                return false;
            }

            return (bool)CanFloatMethod.Invoke(heroController, new object[] { checkControlState });
        }

        private static void TryStartBrollyFromQuickMapInput(HeroController heroController)
        {
            if (!UsesQuickMapInput() ||
                heroController == null ||
                !heroController.CanInput() ||
                heroController.wallLocked ||
                SlideSurface.IsInJumpGracePeriod ||
                !IsGameplayScene(heroController))
            {
                return;
            }

            PlayerAction quickMap = GetQuickMapAction(heroController);
            if (!WasExtraPressed(quickMap) || !CanFloat(heroController, true))
            {
                return;
            }

            if (StartFloatMethod != null)
            {
                StartFloatMethod.Invoke(heroController, null);
                var input = GetInputHandler();
                quickMapConsumedForBrolly = input != null && input.inputActions.QuickMap.IsPressed;
            }
        }

        private static bool IsGameplayScene(HeroController heroController)
        {
            return IsGameplaySceneField != null && (bool)IsGameplaySceneField.GetValue(heroController);
        }

        private static PlayerAction GetQuickMapAction(HeroController heroController)
        {
            InputHandler inputHandler = GetInputHandler();
            return GetExtraAction(inputHandler);
        }

        private static PlayerAction GetQuickMapAction()
        {
            InputHandler inputHandler = GetInputHandler();
            return inputHandler == null ? null : inputHandler.inputActions.QuickMap;
        }

        private static InputHandler GetInputHandler()
        {
            return GameManager.instance == null ? null : GameManager.instance.GetComponent<InputHandler>();
        }

        private static bool IsQuickMapConsumedForBrolly()
        {
            if (!quickMapConsumedForBrolly)
            {
                return false;
            }

            PlayerAction quickMap = GetQuickMapAction();
            if (quickMap == null || quickMap.WasReleased)
            {
                quickMapConsumedForBrolly = false;
                return false;
            }

            return true;
        }

        private static bool ShouldTreatQuickMapAsJump()
        {
            if (!UsesQuickMapInput())
            {
                return false;
            }

            HeroController heroController = HeroController.SilentInstance;
            if (heroController == null || heroController.umbrellaFSM == null)
            {
                return false;
            }

            FsmBool isUmbrellaActive = heroController.umbrellaFSM.FsmVariables.FindFsmBool("Is Active");
            return isUmbrellaActive != null && isUmbrellaActive.Value;
        }

        private static bool TryGetBrollyJumpActions(out PlayerAction jump, out PlayerAction quickMap)
        {
            jump = null;
            quickMap = null;

            if (!ShouldTreatQuickMapAsJump())
            {
                return false;
            }

            InputHandler inputHandler = GetInputHandler();
            if (inputHandler == null)
            {
                return false;
            }

            jump = inputHandler.inputActions.Jump;
            quickMap = GetExtraAction(inputHandler);
            return jump != null;
        }

        private static PlayerAction GetExtraAction(InputHandler input)
        {
            if (input == null || extraAction == null) return null;
            switch (extraAction.Value)
            {
                case ExtraInputAction.QuickMap: return input.inputActions.QuickMap;
                case ExtraInputAction.Cast: return input.inputActions.Cast;
                case ExtraInputAction.QuickCast: return input.inputActions.QuickCast;
                case ExtraInputAction.SuperDash: return input.inputActions.SuperDash;
                case ExtraInputAction.DreamNail: return input.inputActions.DreamNail;
                default: return null;
            }
        }

        private static InputControl ExtraControl => extraController == null || extraController.Value == InputControlType.None
            ? InputControl.Null : InputManager.ActiveDevice.GetControl(extraController.Value);

        private static bool IsExtraPressed(PlayerAction action)
        {
            return (action != null && action.IsPressed) || ExtraControl.IsPressed ||
                (extraKey != null && extraKey.Value != UnityEngine.KeyCode.None && UnityEngine.Input.GetKey(extraKey.Value));
        }

        private static bool WasExtraPressed(PlayerAction action)
        {
            return (action != null && action.WasPressed) || ExtraControl.WasPressed ||
                (extraKey != null && extraKey.Value != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(extraKey.Value));
        }

        private static bool WasExtraReleased(PlayerAction action)
        {
            return (action != null && action.WasReleased) || ExtraControl.WasReleased ||
                (extraKey != null && extraKey.Value != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyUp(extraKey.Value));
        }

        private static bool IsJumpOrQuickMapPressed(PlayerAction jump, PlayerAction quickMap)
        {
            return jump.IsPressed || IsExtraPressed(quickMap);
        }

        private static bool WasJumpOrQuickMapPressed(PlayerAction jump, PlayerAction quickMap)
        {
            return jump.WasPressed || WasExtraPressed(quickMap);
        }

        private static bool WasJumpOrQuickMapReleased(PlayerAction jump, PlayerAction quickMap)
        {
            return (jump.WasReleased || WasExtraReleased(quickMap)) && !IsJumpOrQuickMapPressed(jump, quickMap);
        }

        private static bool HandleListenForJump(ListenForJump action)
        {
            PlayerAction jump = null;
            PlayerAction quickMap = null;
            if (GameManager.instance == null ||
                GameManager.instance.isPaused ||
                action == null ||
                !TryGetBrollyJumpActions(out jump, out quickMap) ||
                (!action.activeBool.IsNone && !action.activeBool.Value))
            {
                return true;
            }

            if (WasJumpOrQuickMapPressed(jump, quickMap))
            {
                action.Fsm.Event(action.wasPressed);
                if (!action.queueBool.IsNone)
                {
                    action.queueBool.Value = true;
                }
            }

            if (WasJumpOrQuickMapReleased(jump, quickMap))
            {
                action.Fsm.Event(action.wasReleased);
            }

            if (IsJumpOrQuickMapPressed(jump, quickMap))
            {
                action.Fsm.Event(action.isPressed);
            }
            else
            {
                action.Fsm.Event(action.isNotPressed);
                if (!action.queueBool.IsNone)
                {
                    action.queueBool.Value = false;
                }
            }

            return false;
        }

        private static bool HandleListenForJumpV2(ListenForJumpV2 action)
        {
            PlayerAction jump = null;
            PlayerAction quickMap = null;
            if (GameManager.instance == null ||
                GameManager.instance.isPaused ||
                action == null ||
                !TryGetBrollyJumpActions(out jump, out quickMap) ||
                (!action.activeBool.IsNone && !action.activeBool.Value))
            {
                return true;
            }

            if (WasJumpOrQuickMapPressed(jump, quickMap))
            {
                action.Fsm.Event(action.wasPressed);
                if (!action.queueBool.IsNone)
                {
                    action.queueBool.Value = true;
                }
            }

            if (WasJumpOrQuickMapReleased(jump, quickMap))
            {
                action.Fsm.Event(action.wasReleased);
            }

            if (IsJumpOrQuickMapPressed(jump, quickMap))
            {
                action.Fsm.Event(action.isPressed);
                action.isPressedBool.Value = true;
            }
            else
            {
                action.Fsm.Event(action.isNotPressed);
                action.isPressedBool.Value = false;
                if (!action.queueBool.IsNone)
                {
                    action.queueBool.Value = false;
                }
            }

            return false;
        }

        private static bool HandleQueueJump(QueueJump action)
        {
            PlayerAction jump = null;
            PlayerAction quickMap = null;
            if (GameManager.instance == null ||
                GameManager.instance.isPaused ||
                action == null ||
                !TryGetBrollyJumpActions(out jump, out quickMap))
            {
                return true;
            }

            if (WasJumpOrQuickMapPressed(jump, quickMap))
            {
                action.queueBool.Value = true;
            }

            if (!IsJumpOrQuickMapPressed(jump, quickMap))
            {
                action.queueBool.Value = false;
            }

            return false;
        }

        private static bool ShouldRunQuickMapListener()
        {
            return !IsQuickMapConsumedForBrolly();
        }

        [HarmonyPatch(typeof(HeroController), "CanFloat")]
        private static class HeroControllerCanFloatPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasBrollyChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "RegainControl", typeof(bool))]
        private static class HeroControllerRegainControlPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasBrollyChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "LookForQueueInput")]
        private static class HeroControllerLookForQueueInputPatch
        {
            private static void Prefix(HeroController __instance)
            {
                TryStartBrollyFromQuickMapInput(__instance);
            }
        }

        [HarmonyPatch(typeof(ListenForJump), "CheckInput")]
        private static class ListenForJumpCheckInputPatch
        {
            private static bool Prefix(ListenForJump __instance)
            {
                return HandleListenForJump(__instance);
            }
        }

        [HarmonyPatch(typeof(ListenForJumpV2), "CheckInput")]
        private static class ListenForJumpV2CheckInputPatch
        {
            private static bool Prefix(ListenForJumpV2 __instance)
            {
                return HandleListenForJumpV2(__instance);
            }
        }

        [HarmonyPatch(typeof(QueueJump), "CheckInput")]
        private static class QueueJumpCheckInputPatch
        {
            private static bool Prefix(QueueJump __instance)
            {
                return HandleQueueJump(__instance);
            }
        }

        [HarmonyPatch]
        private static class HeroControllerCanQuickMapPatches
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return AccessTools.GetDeclaredMethods(typeof(HeroController))
                    .Where(method => method.Name == "CanQuickMap");
            }

            private static void Postfix(ref bool __result)
            {
                if (IsQuickMapConsumedForBrolly())
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(ListenForQuickMap), "OnUpdate")]
        private static class ListenForQuickMapOnUpdatePatch
        {
            private static bool Prefix()
            {
                return ShouldRunQuickMapListener();
            }
        }

        [HarmonyPatch(typeof(ListenForQuickMapV2), "OnUpdate")]
        private static class ListenForQuickMapV2OnUpdatePatch
        {
            private static bool Prefix()
            {
                return ShouldRunQuickMapListener();
            }
        }
    }
}
