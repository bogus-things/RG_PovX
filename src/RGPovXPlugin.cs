using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using UnhollowerRuntimeLib;

namespace RGPovX
{
	[BepInProcess("RoomGirl")]
	[BepInPlugin(GUID, PluginName, Version)]
	public partial class RGPovXPlugin : BasePlugin
	{
		const string GUID = "com.bogus.RGPovX";
		public const string PluginName = "RG PoV X";
		public const string Version = "0.0.3";
		private const string ComponentName = "BogusComponents";

		const string SECTION_GENERAL = "General";
		const string SECTION_CAMERA = "Camera";
		const string SECTION_ANIMATION = "Animation";
		const string SECTION_HOTKEYS = "Hotkeys";

		const string DESCRIPTION_H_SCENE_LOCK_CURSOR =
			"Should the cursor be locked during H scenes? " +
			"Use the 'Cursor Release Key' to reveal the cursor. " +
			"This also include situations where the focus is not the player.";

		const string DESCRIPTION_OFFSET_X =
			"Sideway offset from the character's eyes.";
		const string DESCRIPTION_OFFSET_Y =
			"Vertical offset from the character's eyes.";
		const string DESCRIPTION_OFFSET_Z =
			"Forward offset from the character's eyes.";
		const string DESCRIPTION_CAMERA_NORMALIZE =
			"Stops the camera from tilting. " +
			"Always enabled when locked-on.";

		const string DESCRIPTION_HEAD_MAX_PITCH =
			"Highest upward/downward angle the head can rotate.";
		const string DESCRIPTION_HEAD_MAX_YAW =
			"The farthest the head can rotate left/right";
		const string DESCRIPTION_EYE_MAX_PITCH =
			"Highest upward/downward angle the eyes can rotate.";
		const string DESCRIPTION_EYE_MAX_YAW =
			"The farthest the eyes can rotate left/right";

		const string DESCRIPTION_CHARA_CYCLE_KEY =
			"Switch between characters during PoV mode.";
		const string DESCRIPTION_LOCK_ON_KEY =
			"Lock-on to any of the other characters during PoV mode. " +
			"Press again to cycle between characters or exit lock-on mode.";
		const string DESCRIPTION_CAMERA_DRAG_KEY =
			"During PoV mode, holding down this key will move the camera if the mouse isn't locked.";
		const string DESCRIPTION_CURSOR_TOGGLE_KEY =
			"Pressing this key will force the cursor to be revealed in any scenes. " +
			"Press the key again to turn off.";
		const string DESCRIPTION_HIDE_HEAD =
			"Should the head be invisible when in PoV mode? " +
			"Head is always invisible during H scenes or " +
			"situations where the player can't move.";
		const string DESCRIPTION_HIDE_HEAD_SCALE_Z =
			"Amount to scale Z when hiding head.";
		const string DESCRIPTION_CAMERA_LOCK_HEAD_KEY =
			"During PoV mode in HScenes, pressing this key will lock/unlock the characters head to the default animation position.";
		const string DESCRIPTION_AUTOMATICALLY_LOCK_HEAD =
			"During PoV mode in HScenes, automatically lock/unlock the characters head to the default animation position depending on position.";

		internal static ConfigEntry<bool> HideHead { get; set; }
		internal static ConfigEntry<float> HideHeadScaleZ { get; set; }
		internal static ConfigEntry<bool> HSceneLockCursor { get; set; }
		internal static ConfigEntry<bool> HSceneAutoHeadLock { get; set; }

		internal static ConfigEntry<float> Sensitivity { get; set; }
		internal static ConfigEntry<float> NearClip { get; set; }
		internal static ConfigEntry<float> Fov { get; set; }
		internal static ConfigEntry<float> ZoomFov { get; set; }
		internal static ConfigEntry<float> OffsetX { get; set; }
		internal static ConfigEntry<float> OffsetY { get; set; }
		internal static ConfigEntry<float> OffsetZ { get; set; }
		internal static ConfigEntry<CameraLocation> CameraPoVLocation { get; set; }

		internal static ConfigEntry<float> HeadMaxPitch { get; set; }
		internal static ConfigEntry<float> HeadMaxYaw { get; set; }
		internal static ConfigEntry<float> EyeMaxPitch { get; set; }
		internal static ConfigEntry<float> EyeMaxYaw { get; set; }
		internal static ConfigEntry<bool> CameraNormalize { get; set; }
		internal static ConfigEntry<KeyCode> PovKey { get; set; }
		internal static ConfigEntry<KeyCode> CharaCycleKey { get; set; }
		internal static ConfigEntry<KeyCode> CameraDragKey { get; set; }
		internal static ConfigEntry<KeyCode> CursorToggleKey { get; set; }
		internal static ConfigEntry<KeyCode> ZoomKey { get; set; }
		internal static ConfigEntry<KeyCode> HeadLockKey { get; set; }
		internal static ConfigEntry<KeyCode> LockOnKey { get; set; }
		public enum CameraLocation
        {
			Center,
			LeftEye,
			RightEye
        }

		internal static new ManualLogSource Log;
		public GameObject BogusComponents;

		public override void Load()
		{
			HideHead = Config.Bind(SECTION_GENERAL, "Hide Head", false, DESCRIPTION_HIDE_HEAD);
			HideHeadScaleZ = Config.Bind(SECTION_GENERAL, "Hide Head Scale Z", 0.5f, new ConfigDescription(DESCRIPTION_HIDE_HEAD_SCALE_Z, new AcceptableValueRange<float>(0f, 1f)));
			HSceneLockCursor = Config.Bind(SECTION_GENERAL, "Lock Cursor During H Scenes", false, DESCRIPTION_H_SCENE_LOCK_CURSOR);
			HSceneAutoHeadLock = Config.Bind(SECTION_GENERAL, "Automatically Lock Head During H Scenes", false, DESCRIPTION_AUTOMATICALLY_LOCK_HEAD);

			Sensitivity = Config.Bind(SECTION_CAMERA, "Camera Sensitivity", 2f);
			NearClip = Config.Bind(SECTION_CAMERA, "Camera Near Clip Plane", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f)));
			Fov = Config.Bind(SECTION_CAMERA, "Field of View", 60f);
			ZoomFov = Config.Bind(SECTION_CAMERA, "Zoom Field of View", 15f);
			OffsetX = Config.Bind(SECTION_CAMERA, "Offset X", 0f, DESCRIPTION_OFFSET_X);
			OffsetY = Config.Bind(SECTION_CAMERA, "Offset Y", 0f, DESCRIPTION_OFFSET_Y);
			OffsetZ = Config.Bind(SECTION_CAMERA, "Offset Z", 0f, DESCRIPTION_OFFSET_Z);
			CameraPoVLocation = Config.Bind(SECTION_CAMERA, "Camera Location", CameraLocation.Center);
			CameraNormalize = Config.Bind(SECTION_CAMERA, "Normalize Camera Z-Axis", false, DESCRIPTION_CAMERA_NORMALIZE);

			HeadMaxPitch = Config.Bind(SECTION_ANIMATION, "Max Head Pitch (Up/Down)", 50f, DESCRIPTION_HEAD_MAX_PITCH);
			HeadMaxYaw = Config.Bind(SECTION_ANIMATION, "Max Head Yaw (Left/Right)", 60f, DESCRIPTION_HEAD_MAX_YAW);
			EyeMaxPitch = Config.Bind(SECTION_ANIMATION, "Max Eye Pitch (Up/Down)", 30f, DESCRIPTION_EYE_MAX_PITCH);
			EyeMaxYaw = Config.Bind(SECTION_ANIMATION, "Max Eye Yaw (Left/Right)", 35f, DESCRIPTION_EYE_MAX_YAW);

			PovKey = Config.Bind(SECTION_HOTKEYS, "PoV Toggle Key", KeyCode.Comma);
			CharaCycleKey = Config.Bind(SECTION_HOTKEYS, "Character Cycle Key", KeyCode.Period, DESCRIPTION_CHARA_CYCLE_KEY);
			CameraDragKey = Config.Bind(SECTION_HOTKEYS, "Camera Drag Key", KeyCode.Mouse0, DESCRIPTION_CAMERA_DRAG_KEY);
			CursorToggleKey = Config.Bind(SECTION_HOTKEYS, "Cursor Toggle Key", KeyCode.LeftControl, DESCRIPTION_CURSOR_TOGGLE_KEY);
			ZoomKey = Config.Bind(SECTION_HOTKEYS, "Zoom Key", KeyCode.Z);
			HeadLockKey = Config.Bind(SECTION_HOTKEYS, "Lock Head Key", KeyCode.X, DESCRIPTION_CAMERA_LOCK_HEAD_KEY);
			LockOnKey = Config.Bind(SECTION_HOTKEYS, "Lock-On Key", KeyCode.Semicolon, DESCRIPTION_LOCK_ON_KEY);

			CameraPoVLocation.SettingChanged += (sender, args) => { PovController.CalculateEyesOffset(); };
			HideHead.SettingChanged += (sender, args) => { PovController.AdjustPoVHeadScale(); };
			HideHeadScaleZ.SettingChanged += (sender, args) => { PovController.AdjustPoVHeadScale(); };

			HSceneLockCursor.SettingChanged += (sender, args) =>
			{
				if (PovController.povEnabled && !HSceneLockCursor.Value)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			};

			Log = base.Log;

			ClassInjector.RegisterTypeInIl2Cpp<PovController>();

			BogusComponents = GameObject.Find(ComponentName);
			if (BogusComponents == null)
			{
				BogusComponents = new GameObject(ComponentName);
				GameObject.DontDestroyOnLoad(BogusComponents);
				BogusComponents.hideFlags = HideFlags.HideAndDontSave;
			}
			BogusComponents.AddComponent(Il2CppType.Of<PovController>());

			HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
		}
	}
}
