using UnityEngine;


namespace CrowRx.Singleton
{
    using Utility;


    public abstract class StaticSceneObject<TComponent> : SceneObject<TComponent>
        where TComponent : StaticSceneObject<TComponent>, IInstance
    {
        public new static TComponent Instance
        {
            get
            {
                if (_instance is not null)
                {
                    return _instance;
                }

                TComponent[] objects = FindObjectsByType<TComponent>(FindObjectsSortMode.None);

                if (objects == null || objects.Length == 0)
                {
                    _instance = new GameObject(TypeName).AddComponent<TComponent>();
                }
                else
                {
                    if (objects.Length > 1)
                    {
                        Log.Error($"[{TypeName}] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                    }

                    _instance = objects[0];
                }

                _instance.Init();

                DontDestroyOnLoad(_instance.gameObject);

                return _instance;
            }
        }

        private static string TypeName => $"StaticSceneObject<{typeof(TComponent).Name}>";
    }
}