// ReSharper disable InconsistentNaming

using System;


namespace CrowRx.Singleton
{
    public class Native<TInstance>
        where TInstance : Native<TInstance>, IInstance
    {
        private static TInstance _instance;
        

        public static TInstance Instance
        {
            get
            {
                if (IsValid)
                {
                    return _instance;
                }

                _instance = Activator.CreateInstance<TInstance>();
                _instance.Init();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
                return _instance;
            }
        }

        public static bool IsValid => _instance is not null;

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playMode)
        {
            if (playMode != UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            _instance?.Release();
            _instance = null;
        }
#endif
    }
}