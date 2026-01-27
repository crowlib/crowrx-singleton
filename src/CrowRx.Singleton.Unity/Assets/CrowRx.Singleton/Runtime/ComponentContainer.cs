// ReSharper disable InconsistentNaming

using System;
using UnityEngine;
using R3;
using R3.Triggers;


namespace CrowRx.Singleton
{
    using Utility;
    using Object = UnityEngine.Object;


    /// <summary>
    /// <see cref="MonoBehaviour"/>를 상속받지 않는 singleton container
    /// </summary>
    public class ComponentContainer<TComponent>
        where TComponent : Component, IInstance
    {
        protected static TComponent _instance;


        public static TComponent Instance
        {
            get
            {
                if (_instance is not null)
                {
                    return _instance;
                }

                try
                {
                    TComponent[] objects = Object.FindObjectsByType<TComponent>(FindObjectsSortMode.None);

                    if (objects == null || objects.Length == 0)
                    {
                        throw new Exception($"There is no <{typeof(TComponent).Name}> exists in this scene.");
                    }

                    if (objects.Length > 1)
                    {
                        Log.Error($"[ComponentContainer<{typeof(TComponent).Name}>] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                    }

                    _instance = objects[0];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                if (_instance)
                {
                    _instance
                        .OnDestroyAsObservable()
                        .Subscribe(_ =>
                        {
                            if (_instance)
                            {
                                _instance.Release();
                                _instance = null;
                            }
                        });

                    _instance.Init();
                }

                return _instance;
            }
        }

        public static bool IsValid => _instance;
    }
}