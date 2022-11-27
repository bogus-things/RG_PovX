using BepInEx.Logging;
using Chara;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;

namespace RGPovX
{
	public class PovController : MonoBehaviour
	{
		private static ManualLogSource Log = RGPovXPlugin.Log;

		public static bool povEnabled = false;
		public static bool showCursor = false;

		// total camera rotation relative to body forward
		public static float cameraLocalPitch = 0f;
		public static float cameraLocalYaw = 0f;
		// portion of camera rotation that is acheived through head/neck rotation
		public static float headLocalPitch = 0f;
		public static float headLocalYaw = 0f;
		// portion of camera rotation that is acheived through eye rotation
		public static float eyeLocalPitch = 0f;
		public static float eyeLocalYaw = 0f;

		// 0 = Player; 1 = 1st Partner; 2 = 2nd Partner; 3 = ...
		public static int povFocus = 0;
		public static int targetFocus = 0;
		public static ChaControl[] characters = new ChaControl[0];
		public static ChaControl povCharacter;
		public static ChaControl targetCharacter;
		public static Vector3 eyeOffset = Vector3.zero;
		public static Vector3 normalHeadScale;
		public static float backupFoV;
		public static bool backupShield;

		public static bool inScene;
		public static bool lockHeadPosition;
		public static bool povSetThisFrame = false;

		public static HScene hScene;
		public static string currentHMotion;

		private static readonly List<string> firstMaleLockHeadAllHPositions = new List<string>() { "aia_f_10", "h2h_f_03", "ait_f_00", "ait_f_07" };
		private static readonly List<string> firstMaleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_04", "aia_f_06", "aia_f_07", "aia_f_08", "aia_f_11", "aia_f_12", "aia_f_13", "aia_f_18", "aia_f_19", "aia_f_23", "aia_f_24", "aia_f_26", "ai3p", "h2a_f_00" };
		private static readonly List<string> secondMaleLockHeadAllHPositions = new List<string>() { "h2_m2f_f_00", "h2_m2f_f_02" };
		private static readonly List<string> secondMaleLockHeadHPositions = new List<string>() { };

		private static readonly List<string> firstFemaleLockHeadAllHPositions = new List<string>() { "ais_f_19", "aia_f_16", "ais_f_27" };
		private static readonly List<string> firstFemaleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_07", "aia_f_11", "aia_f_12", "aih_f_00", "aih_f_04", "aih_f_05", "aih_f_09", "aih_f_10", "aih_f_12", "aih_f_13", "aih_f_14", "aih_f_16", "aih_f_17", "aih_f_19", "aih_f_21", "aih_f_23", "aih_f_25", "aih_f_26", "aih_f_27", "h2h_f_02", "h2h_f_03", "aih_f_06", "aih_f_07", "ail_f1_03", "ail_f1_04", "h2_mf2_f1_00", "h2_mf2_f2_03", "h2_m2f_f_01", "h2_m2f_f_04", "h2_m2f_f_05", "h2_m2f_f_06", "ait_f_07" };
		private static readonly List<string> secondFemaleLockHeadAllHPositions = new List<string>() { };
		private static readonly List<string> secondFemaleLockHeadHPositions = new List<string>() { "ail_f2_03", "ail_f2_04", "h2_mf2_f1_00", "h2_mf2_f2_03" };

		private static readonly List<List<string>> LockHeadAllHPositions = new List<List<string>>() { firstMaleLockHeadAllHPositions, secondMaleLockHeadAllHPositions, firstFemaleLockHeadAllHPositions, secondFemaleLockHeadAllHPositions };
		private static readonly List<List<string>> LockHeadHPositions = new List<List<string>>() { firstMaleLockHeadHPositions, secondMaleLockHeadHPositions, firstFemaleLockHeadHPositions, secondFemaleLockHeadHPositions };

		private static readonly List<string> lockHeadHMotionExceptions = new List<string>() { "Idle", "_A" };

		internal static readonly string lowerNeckBone = "cf_J_Neck";
		internal static readonly string upperNeckBone = "cf_J_Head";
		internal static readonly string headBone = "cf_J_Head_s";
		internal static readonly string lockBone = "N_Hitai";
		internal static readonly string leftEyeBone = "cf_J_eye_rs_L";
		internal static readonly string rightEyeBone = "cf_J_eye_rs_R";
		internal static readonly string leftEyePupil = "cf_J_pupil_s_L";
		internal static readonly string rightEyePupil = "cf_J_pupil_s_R";

		internal static Transform povUpperNeck;
		internal static Transform povLowerNeck;
		internal static Transform povHead;
		internal static Transform lockTarget;

		

		public void Update()
		{
			povSetThisFrame = false;

			if (Input.GetKeyDown(RGPovXPlugin.PovKey.Value))
				EnablePoV(!povEnabled);

			if (!povEnabled)
				return;

			if (Input.GetKeyDown(RGPovXPlugin.CharaCycleKey.Value))
			{
				targetFocus = povFocus = GetValidFocus(povFocus + 1);
				SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
			}

			if (Input.GetKeyDown(RGPovXPlugin.HeadLockKey.Value))
				LockPoVHead(!lockHeadPosition);

			if (Input.GetKeyDown(RGPovXPlugin.LockOnKey.Value))
			{
				targetFocus = GetValidFocus(targetFocus + 1);
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
			}

			if (Input.GetKeyDown(RGPovXPlugin.CursorToggleKey.Value))
			{
				Cursor.visible = !Cursor.visible;
				Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
			}
			else if (!Input.GetKey(RGPovXPlugin.ZoomKey.Value) && !Cursor.visible && Input.AnyKeyDown())
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}

			if (povFocus == targetFocus)
				UpdateMouseLook();
		}

		public static void ResetPoVRotations()
        {
			ResetPoVPitch();
			ResetPoVYaw();
		}

		public static void ResetPoVPitch()
		{
			cameraLocalPitch = headLocalPitch = eyeLocalPitch = 0f;
		}

		public static void ResetPoVYaw()
		{
			cameraLocalYaw = headLocalYaw = eyeLocalYaw = 0f;
		}

		public static void EnablePoV(bool enable)
		{
			if (povEnabled == enable)
				return;

			if (enable)
			{
				characters = GetSceneCharacters();

				if (characters.Length == 0)
					return;

				if (!FocusCharacterValid(povFocus))
					targetFocus = povFocus = GetValidFocus(povFocus + 1);

				if (!FocusCharacterValid(targetFocus))
					targetFocus = GetValidFocus(targetFocus + 1);

				SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
				ResetPoVRotations();
				backupFoV = Camera.main.fieldOfView;

				backupShield = Manager.Config.GraphicData.Shield;
				Manager.Config.GraphicData.Shield = false;				
			}
			else
			{
				characters = new ChaControl[0];
				if (RGPovXPlugin.HSceneLockCursor.Value)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}

				if (Camera.main != null)
                {
					Camera.main.fieldOfView = backupFoV;
				}
				
				Manager.Config.GraphicData.Shield = backupShield;
				SetPoVCharacter(null);
				SetTargetCharacter(null);
			}

			povEnabled = enable;
		}

		public static ChaControl[] GetSceneCharacters()
		{
            // If in an H scene, grab the list of participants
            if (hScene != null)
            {
                Il2CppReferenceArray<ChaControl> females = hScene.GetFemales();
                Il2CppReferenceArray<ChaControl> males = hScene.GetMales();

                List<ChaControl> chars = new List<ChaControl>();

                foreach (ChaControl female in females)
                {
                    chars.Add(female);
                }

                foreach (ChaControl male in males)
                {
                    chars.Add(male);
                }

                return chars.ToArray();
            }

            // If in the main game, grab all the actors in the scene
            GameObject actorGroup = GameObject.Find("ActorGroup");
            if (actorGroup != null)
            {
                return actorGroup.GetComponentsInChildren<ChaControl>();
            }

            return new ChaControl[0];
		}

		public static void SetPoVCharacter(ChaControl character)
		{
			if (povCharacter == character)
				return;

			if (povCharacter != null)
			{
				povUpperNeck.localRotation = Quaternion.identity;
				povLowerNeck.localRotation = Quaternion.identity;
				povHead.localRotation = Quaternion.identity;
				eyeOffset = Vector3.zero;

				if (normalHeadScale != null)
					povCharacter.ObjHeadBone.transform.localScale = normalHeadScale;
			}

			povCharacter = character;
			if (povCharacter == null)
				return;

			povUpperNeck = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(upperNeckBone)).FirstOrDefault();
			povLowerNeck = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(lowerNeckBone)).FirstOrDefault();
			povHead = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(headBone)).FirstOrDefault();
			normalHeadScale = povCharacter.ObjHeadBone.transform.localScale;

			CalculateEyesOffset();
			AdjustPoVHeadScale();
			CheckHSceneHeadLock();
		}

		public static void SetTargetCharacter(ChaControl character)
		{
			if (targetCharacter == character)
				return;
		
			targetCharacter = character;
			if (targetCharacter == null)
				return;

			lockTarget = targetCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(lockBone)).FirstOrDefault();
		}

		public static int GetValidFocus(int focus)
		{
			if (focus >= characters.Length)
				focus %= characters.Length;

			for (int i = 0; i < characters.Length; i++)
			{
				if (FocusCharacterValid(focus))
					return focus;

				// Skip invisible or destroyed characters.
				focus = (focus + 1) % characters.Length;
			}

			return focus;
		}

		public static bool FocusCharacterValid(int focus)
		{
			if (focus >= characters.Length)
				return false;

			var focusCharacter = characters[focus];
			if (focusCharacter != null && focusCharacter.VisibleAll)
				return true;

			return false;
		}

		public static ChaControl GetValidCharacterFromFocus(ref int focus)
		{
			if (characters.Length == 0)
				return null;

			focus = GetValidFocus(focus);
			return characters[focus];
		}

		public static void UpdateTargetLockedCamera(Transform head)
		{
			UpdateCamera(head);
			Camera.main.transform.LookAt(lockTarget.position, Vector3.up);
		}

		public static void UpdateCamera(Transform head, Vector3 offsetRotation)
		{
			UpdateCamera(head);

			if (RGPovXPlugin.CameraNormalize.Value)
				Camera.main.transform.rotation = Quaternion.Euler(head.eulerAngles.x, head.eulerAngles.y, 0);
			else
				Camera.main.transform.rotation = head.rotation;

			Camera.main.transform.Rotate(offsetRotation);		
		}

		public static void UpdateCamera(Transform head)
		{
			Camera.main.fieldOfView =
				Input.GetKey(RGPovXPlugin.ZoomKey.Value) ?
					RGPovXPlugin.ZoomFov.Value :
					RGPovXPlugin.Fov.Value;

			Camera.main.transform.position =
				head.position +
				(RGPovXPlugin.OffsetX.Value + eyeOffset.x) * head.right +
				(RGPovXPlugin.OffsetY.Value + eyeOffset.y) * head.up +
				(RGPovXPlugin.OffsetZ.Value + eyeOffset.z) * head.forward;

			Camera.main.nearClipPlane = RGPovXPlugin.NearClip.Value;
		}

		public static void UpdatePoVCamera()
		{
			UpdatePoVHScene();
			povSetThisFrame = true;
		}

		public static void UpdatePoVHScene()
		{
			if (povFocus != targetFocus)
			{
				UpdateTargetLockedCamera(povHead);
				return;
			}

			if (!lockHeadPosition)
				UpdateNeckRotations();

			UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
		}

		public static void CheckHSceneHeadLock(string hMotion = null)
		{
			if (!RGPovXPlugin.HSceneAutoHeadLock.Value ||hScene == null || povFocus >= LockHeadHPositions.Count)
				return;

			string currentHAnimation = hScene.CtrlFlag.NowAnimationInfo.FileFemale;

			if (hMotion != null)
				currentHMotion = hMotion;

			if (currentHAnimation == null || currentHMotion == null)
				return;

			if (LockHeadAllHPositions[povFocus].Contains(currentHAnimation) ||
				(LockHeadHPositions[povFocus].Contains(currentHAnimation) && !lockHeadHMotionExceptions.Contains(currentHMotion)))
				LockPoVHead(true);
			else
				LockPoVHead(false);
		}

		public static void LockPoVHead(bool locked)
        {
			lockHeadPosition = locked;

			if (locked)
				ResetPoVRotations();

		}

		public static void AdjustPoVHeadScale()
		{
			if (povCharacter == null)
				return;

			if (Tools.ShouldHideHead())
				povCharacter.ObjHeadBone.transform.localScale = new Vector3(povCharacter.ObjHeadBone.transform.localScale.x, povCharacter.ObjHeadBone.transform.localScale.y, RGPovXPlugin.HideHeadScaleZ.Value);
			else
				povCharacter.ObjHeadBone.transform.localScale = normalHeadScale;
		}

		public static void CalculateEyesOffset()
		{
			if (povCharacter == null)
				return;

			eyeOffset = Tools.GetEyesOffset(povCharacter);
		}

		private static void UpdateNeckRotations()
		{
			if (povUpperNeck == null || povLowerNeck == null)
				return;

			povLowerNeck.localRotation = Quaternion.Euler(headLocalPitch / 2, headLocalYaw / 2, 0);
			povUpperNeck.localRotation = Quaternion.Euler(headLocalPitch / 2, headLocalYaw / 2, 0);
		}

		private static void UpdateMouseLook()
		{
			if (Cursor.lockState == CursorLockMode.None && !Input.GetKey(RGPovXPlugin.CameraDragKey.Value))
				return;

			float sensitivity = RGPovXPlugin.Sensitivity.Value;

			if (Input.GetKey(RGPovXPlugin.ZoomKey.Value))
				sensitivity *= RGPovXPlugin.ZoomFov.Value / RGPovXPlugin.Fov.Value;

			float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
			float mouseX = Input.GetAxis("Mouse X") * sensitivity;

			if (lockHeadPosition)
			{
				eyeLocalPitch = cameraLocalPitch = Mathf.Clamp(cameraLocalPitch - mouseY, -(RGPovXPlugin.EyeMaxPitch.Value), (RGPovXPlugin.EyeMaxPitch.Value));
				eyeLocalYaw = cameraLocalYaw = Mathf.Clamp(cameraLocalYaw + mouseX, -(RGPovXPlugin.EyeMaxYaw.Value), (RGPovXPlugin.EyeMaxYaw.Value));
				headLocalPitch = 0;
				headLocalYaw = 0;
			}
			else
			{
				cameraLocalPitch = Mathf.Clamp(cameraLocalPitch - mouseY, -(RGPovXPlugin.EyeMaxPitch.Value + RGPovXPlugin.HeadMaxPitch.Value), (RGPovXPlugin.EyeMaxPitch.Value + RGPovXPlugin.HeadMaxPitch.Value));
				cameraLocalYaw = Mathf.Clamp(cameraLocalYaw + mouseX, -(RGPovXPlugin.EyeMaxYaw.Value + RGPovXPlugin.HeadMaxYaw.Value), (RGPovXPlugin.EyeMaxYaw.Value + RGPovXPlugin.HeadMaxYaw.Value));
				headLocalPitch = cameraLocalPitch * RGPovXPlugin.HeadMaxPitch.Value / (RGPovXPlugin.EyeMaxPitch.Value + RGPovXPlugin.HeadMaxPitch.Value);
				headLocalYaw = cameraLocalYaw * RGPovXPlugin.HeadMaxYaw.Value / (RGPovXPlugin.EyeMaxYaw.Value + RGPovXPlugin.HeadMaxYaw.Value);
				eyeLocalPitch = cameraLocalPitch - headLocalPitch;
				eyeLocalYaw = cameraLocalYaw - headLocalYaw;
			}
		}


	}
}
