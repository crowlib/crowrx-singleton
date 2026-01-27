// ReSharper disable InconsistentNaming

using System;
using UnityEngine;


namespace CrowRx.Singleton
{
    using Utility;


    public abstract class SceneObject<TComponent> : MonoBehaviourCrowRx, IInstance
        where TComponent : SceneObject<TComponent>, IInstance
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
                    TComponent[] objects = FindObjectsByType<TComponent>(FindObjectsSortMode.None);

                    if (objects == null || objects.Length == 0)
                    {
                        throw new Exception($"There is no <{typeof(TComponent).Name}> exists in this scene.");
                    }

                    if (objects.Length > 1)
                    {
                        Log.Error($"[SceneObject<{typeof(TComponent).Name}>] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                    }

                    _instance = objects[0];
                    _instance.Init();
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                return _instance;
            }
        }

        public static bool IsValid => _instance;


        // If no other MonoBehaviour request the instance in an Awake function
        // executing before this one, no need to search the object.
        private void Awake()
        {
            if (IsValid)
            {
                return;
            }

            _instance = this as TComponent;
            _instance?.Init();
        }

        private void OnDestroy()
        {
            if (_instance)
            {
                _instance.Release();
                _instance = null;
            }
        }

        private void Start() => OnStart();

        // Make sure the instance isn't referenced anymore when the user quit, just in case.
        private void OnApplicationQuit() => OnDestroy();

        // This function is called when the instance is used the first time
        // Put all the initializations you need here, as you would do in Awake
        public void Init() => OnInit();
        public void Release() => OnRelease();

        protected virtual void OnInit()
        {
        }

        protected virtual void OnRelease()
        {
        }

        protected virtual void OnStart()
        {
        }
    }
}