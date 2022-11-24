using System;
using System.Reflection;
using UnityEngine;
using UniverseLib;
using UniverseLib.Input;

namespace RGPovX
{
    internal static class Input
    {
		private static Type TInput
		{
			get
			{
				if (t_Input == null)
				{
					return t_Input = ReflectionUtility.GetTypeByName("UnityEngine.Input");
				}
				return t_Input;
			}
		}
		private static Type t_Input;

		private static MethodInfo MGetAxis
		{
			get
			{
				if (TInput == null)
				{
					return null;
				}

				if (m_getAxis == null)
				{
					return m_getAxis = TInput.GetMethod("GetAxis", new Type[] { typeof(string) });
				}
				return m_getAxis;
			}
		}
		private static MethodInfo m_getAxis;

		private static PropertyInfo PAnyKeyDown
        {
			get
            {
				if (TInput == null)
                {
					return null;
                }

				if (p_anyKeyDown == null)
                {
					return p_anyKeyDown = TInput.GetProperty("anyKeyDown");
                }
				return p_anyKeyDown;
            }
        }
		private static PropertyInfo p_anyKeyDown;

		internal static bool GetKeyDown(KeyCode key)
        {
            return InputManager.GetKeyDown(key);
        }

        internal static bool GetKey(KeyCode key)
        {
            return InputManager.GetKey(key);
        }

		internal static float GetAxis(string axisName)
		{
			MethodInfo getAxis = MGetAxis;
			if (getAxis != null)
			{
				return (float)getAxis.Invoke(null, new object[] { axisName });
			}
			return 0;
		}

		internal static bool AnyKeyDown()
        {
			PropertyInfo anyKeyDown = PAnyKeyDown;
			if (anyKeyDown != null)
            {
				return (bool)anyKeyDown.GetValue(null, null);
            }
			return false;
        }
	}
}
