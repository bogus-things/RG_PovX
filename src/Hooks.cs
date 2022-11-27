using BepInEx.Logging;
using Chara;
using HarmonyLib;
using Illusion.Unity.Animations;
using RG.Scene;

namespace RGPovX
{
	internal static class Hooks
	{
		private static ManualLogSource Log = RGPovXPlugin.Log;

		[HarmonyPrefix, HarmonyPatch(typeof(NeckLookControllerVer2), nameof(NeckLookControllerVer2.LateUpdate))]
		public static bool Prefix_NeckLookControllerVer2_LateUpdate(NeckLookControllerVer2 __instance)
		{
			if (!PovController.povEnabled ||
				PovController.povCharacter == null ||
				PovController.povSetThisFrame ||
				PovController.povCharacter.NeckLookCtrl.enabled && __instance != PovController.povCharacter.NeckLookCtrl)
				return true;

			PovController.UpdatePoVCamera();
			return false;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
		public static void HScene_Post_Start(HScene __instance)
		{
			PovController.hScene = __instance;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), nameof(HScene.ChangeAnimation))]
		public static void HScene_Post_ChangeAnimation()
		{
			PovController.CheckHSceneHeadLock();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), nameof(HScene.SetMovePositionPoint))]
		public static void HScene_Post_SetMovePositionPoint()
        {
			PovController.CheckHSceneHeadLock();
		}

		[HarmonyPrefix, HarmonyPatch(typeof(HScene), nameof(HScene.OnDestroy))]
		public static void HScene_Pre_OnDestroy()
        {
			PovController.EnablePoV(false);
			PovController.hScene = null;
        }

		[HarmonyPrefix, HarmonyPatch(typeof(ExitDialog), nameof(ExitDialog.Open))]
        public static void ExitDialog_Pre_Open()
        {
			PovController.EnablePoV(false);
        }

		[HarmonyPrefix, HarmonyPatch(typeof(ActionScene), nameof(ActionScene.OnDestroy))]
		public static void ActionScene_Pre_OnDestroy()
		{
			PovController.EnablePoV(false);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Play))]
		public static void ChaControl_Post_SetPlay(string _strAnmName)
		{
			if (string.IsNullOrEmpty(_strAnmName))
				return;

			PovController.CheckHSceneHeadLock(_strAnmName);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.AnimPlay))]
		public static void ChaControl_Post_AnimPlay(string stateName)
		{
			if (string.IsNullOrEmpty(stateName))
				return;

			PovController.CheckHSceneHeadLock(stateName);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.PlaySync), typeof(string), typeof(int), typeof(float))]
		public static void ChaControl_Post_SyncPlay(string _strameHash)
		{
			if (string.IsNullOrEmpty(_strameHash))
				return;

			PovController.CheckHSceneHeadLock(_strameHash);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(CameraControl_Ver2), nameof(CameraControl_Ver2.LateUpdate))]
		public static bool Prefix_CameraControl_Ver2_LateUpdate()
		{
			return !PovController.povEnabled;
		}
	}
}
