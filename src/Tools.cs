using Chara;
using System.Linq;
using UnityEngine;

namespace RGPovX
{
    public static class Tools
	{
		public static bool ShouldHideHead()
		{
			return PovController.povEnabled && RGPovXPlugin.HideHead.Value;
		}

		// Return the offset of the eyes in the head's object space.
		public static Vector3 GetEyesOffset(ChaInfo chaCtrl)
		{
			Transform head = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(PovController.headBone)).FirstOrDefault();

			Transform[] eyes = new Transform[2];
			eyes[0] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(PovController.leftEyePupil)).FirstOrDefault();
			eyes[1] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(PovController.rightEyePupil)).FirstOrDefault();

			if (RGPovXPlugin.CameraPoVLocation.Value == RGPovXPlugin.CameraLocation.LeftEye)
				return GetEyesOffsetInternal(head, eyes[0]);
			else if (RGPovXPlugin.CameraPoVLocation.Value == RGPovXPlugin.CameraLocation.RightEye)
				return GetEyesOffsetInternal(head, eyes[1]);

			return Vector3.Lerp(
				GetEyesOffsetInternal(head, eyes[0]),
				GetEyesOffsetInternal(head, eyes[1]),
				0.5f);
		}
		
		private static Vector3 GetEyesOffsetInternal(Transform head, Transform eye)
		{
			Vector3 offset = Vector3.zero;

			for (int bone = 0; bone < 50; bone++)
			{
				if (eye == null || eye == head)
					break;

				offset += eye.localPosition;
				eye = eye.parent;
			}

			return offset;
		}
	}
}
