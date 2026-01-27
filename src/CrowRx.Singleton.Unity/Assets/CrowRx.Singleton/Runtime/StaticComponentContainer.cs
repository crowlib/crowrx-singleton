using UnityEngine;
using R3;
using R3.Triggers;


namespace CrowRx.Singleton
{
    using Utility;


    /// <summary>
    /// <see cref="MonoBehaviour"/>를 상속받지 않는 singleton container. <see cref="Object.DontDestroyOnLoad"/> 사용
    /// </summary>
    public class StaticComponentContainer<TComponent> : ComponentContainer<TComponent>
        where TComponent : Component, IInstance
    {
        public new static TComponent Instance
        {
            get
            {
                if (_instance is not null)
                {
                    return _instance;
                }

                TComponent[] objects = Object.FindObjectsByType<TComponent>(FindObjectsSortMode.None);

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

                if (_instance)
                {
                    _instance.OnDestroyAsObservable()
                        .Subscribe(_ =>
                        {
                            if (_instance)
                            {
                                _instance.Release();
                                _instance = null;
                            }
                        });

                    _instance.Init();

                    Object.DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        private static string TypeName => $"StaticComponentContainer<{typeof(TComponent).Name}>";
    }
}