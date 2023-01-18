using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using UnityEngine.Rendering.PostProcessing;

namespace PostProcessingDisabler
{
    public class PostProcessingDisabler : MelonMod
    {
        private static MethodInfo s_joinMethod { get; set; }
        private delegate IntPtr userJoined(IntPtr _instance, IntPtr _user, IntPtr _methodinfo);
        private static userJoined s_userJoined;
        public static PostProcessVolume[] s_PostProcess { get; set; }

        public override void OnApplicationStart()
        {
            MelonLogger.Msg(ConsoleColor.Yellow, "Post Processing Will Be Disbaled On World Join By Default");
            MelonLogger.Msg(ConsoleColor.Cyan, "Keybind: RightCRTL + P To ReEnable Post Processing");
            MelonLogger.Msg(ConsoleColor.Red, "Please Note: ReEnabling In Some Worlds Might Create Undesired Lighting As It Enables All Post Processing");
            NativeHook();
        }

        public override void OnUpdate()
        {
            if (Input.GetKey(KeyCode.RightControl) & Input.GetKeyDown(KeyCode.P))
            {
                s_PostProcess = UnityEngine.Object.FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
                if (s_PostProcess != null)
                {
                    MelonLogger.Msg(ConsoleColor.Green, "ReEnabled Post Processing");
                    foreach (var pp in s_PostProcess)
                    {
                        pp.enabled = true;
                    }
                }
            }
        }

        private unsafe void NativeHook()
        {
            var methodInfos = typeof(NetworkManager).GetMethods().Where(x => x.Name.StartsWith("Method_Public_Void_Player_")).ToArray();

            for (int i = 0; i < methodInfos.Length; i++)
            {
                var mt = UnhollowerRuntimeLib.XrefScans.XrefScanner.XrefScan(methodInfos[i]).ToArray();
                for (int j = 0; j < mt.Length; j++)
                {
                    if (mt[j].Type != UnhollowerRuntimeLib.XrefScans.XrefType.Global) continue;

                    if (mt[j].ReadAsObject().ToString().Contains("OnPlayerJoin"))
                    {
                        var methodPointer = *(IntPtr*)(IntPtr)UnhollowerBaseLib.UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(methodInfos[i]).GetValue(null);
                        MelonUtils.NativeHookAttach((IntPtr)(&methodPointer), typeof(PostProcessingDisabler).GetMethod(nameof(OnJoin), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
                        s_userJoined = Marshal.GetDelegateForFunctionPointer<userJoined>(methodPointer);
                    }
                }
            }
        }

        private static void PostProcessOnJoin()
        {
            s_PostProcess = UnityEngine.Object.FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            if (s_PostProcess != null)
            {
                foreach (var pp in s_PostProcess)
                {
                    pp.enabled = false;
                }
            }
        }

        private static void OnJoin(IntPtr _instance, IntPtr _user, IntPtr _methodInfo)
        {
            s_userJoined(_instance, _user, _methodInfo);
            PostProcessOnJoin();
        }
    }
}
