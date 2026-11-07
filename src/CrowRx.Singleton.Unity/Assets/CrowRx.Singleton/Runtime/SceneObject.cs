using System;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace CrowRx.Singleton
{
    /// <summary>
    /// 씬에 배치된 컴포넌트를 타입별 단일 인스턴스로 제공하는 기반 클래스입니다.
    /// </summary>
    /// <typeparam name="TComponent">자기 자신을 타입 인수로 지정하는 파생 컴포넌트 타입입니다.</typeparam>
    /// <remarks>
    /// Awake 또는 Instance 접근 시 OnInit을 수행하고, 초기화에 성공한 객체를 공개합니다.
    /// Instance는 비활성 객체도 검색하며, 객체를 새로 생성하지 않습니다.
    /// 검색 결과가 여러 개이면 활성화되고 사용 가능한 컴포넌트를 우선 선택하고,
    /// 해당 컴포넌트가 없으면 첫 번째 객체를 선택합니다.
    /// 현재 인스턴스의 파괴 또는 ReleaseInstance 호출 시 OnRelease를 수행합니다.
    /// 한 번도 활성화되지 않은 객체의 파괴는 다음 Instance 접근 또는 명시적 ReleaseInstance 호출 시 정리합니다.
    /// Unity의 Start 시점에 보류된 OnStart는 초기화에 성공하고 활성 상태가 되면 수행합니다.
    /// Unity 메인 스레드에서 사용하며, 파생 클래스는 OnInit, OnRelease, OnStart를 재정의하여
    /// 초기화, 정리 및 시작 동작을 구현합니다.
    /// </remarks>
    public abstract partial class SceneObject<TComponent> : SceneObjectBase
        where TComponent : SceneObject<TComponent>
    {
        #region static

        [AutoStaticsCleanup] private static TComponent _instance;
        [AutoStaticsCleanup] private static bool _isInitializing;
        [AutoStaticsCleanup] private static bool _isReleasing;

        public static TComponent Instance
        {
            get
            {
                if (_isInitializing || _isReleasing)
                {
                    throw new InvalidOperationException($"Cannot access {typeof(TComponent).Name}.Instance during initialization or release.");
                }

                if (_instance)
                {
                    return _instance;
                }

                // A never-active object may be destroyed without receiving OnDestroy.
                ReleaseInstance();

                _isInitializing = true;

                try
                {
                    TComponent[] objects = FindObjectsByType<TComponent>(FindObjectsInactive.Include);
                    if (objects is null || objects.Length == 0)
                    {
                        throw new Exception($"There is no <{typeof(TComponent).Name}> exists in this scene.");
                    }

                    TComponent instance = null;

                    switch (objects.Length)
                    {
                        case 1:
                            instance = objects[0];
                            break;
                        case > 1:
                            int availableComponentCount = 0;
                            foreach (TComponent component in objects)
                            {
                                if (component.isActiveAndEnabled)
                                {
                                    ++availableComponentCount;

                                    instance ??= component;
                                }
                            }

                            if (availableComponentCount > 1)
                            {
                                Log.Error($"[SceneObject<{typeof(TComponent).Name}>] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                            }

                            instance ??= objects[0];

                            break;
                    }

                    if (instance)
                    {
                        instance.Initialize();
                        _instance = instance;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
                finally
                {
                    _isInitializing = false;
                }

                if (_instance && _instance._startDeferred)
                {
                    _instance.ResumeStart();
                }

                return _instance;
            }
        }

        public static bool IsValid => _instance;

        public static void ReleaseInstance()
        {
            if (_isInitializing || _isReleasing)
            {
                throw new InvalidOperationException($"Cannot access {typeof(TComponent).Name}.ReleaseInstance during initialization or release.");
            }

            TComponent instance = _instance;
            if (instance is null)
            {
                return;
            }

            _isReleasing = true;
            _instance = null;

            try
            {
                instance.OnRelease();
            }
            finally
            {
                _isReleasing = false;
            }
        }

        #endregion

        private bool _startDeferred;
        private bool _disabledForDeferredStart;

        private protected virtual bool PersistAcrossScenes => false;

        #region unity event

        // If no other MonoBehaviour request the instance in an Awake function
        // executing before this one, no need to search the object.
        protected sealed override void Awake()
        {
            if (IsValid || _isInitializing || _isReleasing)
            {
                return;
            }

            // Release a destroyed predecessor before publishing its replacement.
            ReleaseInstance();

            _isInitializing = true;

            try
            {
                Initialize();

                _instance = this as TComponent;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        protected sealed override void OnDestroy()
        {
            if (this == _instance)
            {
                ReleaseInstance();
            }
        }

        protected sealed override void OnEnable() => ResumeStart();

        protected sealed override void Start()
        {
            if (this != _instance)
            {
                _startDeferred = true;
                _disabledForDeferredStart = true;
                enabled = false;
                return;
            }

            OnStart();
        }

        #endregion

        private void Initialize()
        {
            if (PersistAcrossScenes && transform.parent != null)
            {
                throw new InvalidOperationException($"{GetType().Name} must be attached to a root GameObject to persist across scenes.");
            }

            bool restoreEnabled = _disabledForDeferredStart;
            _disabledForDeferredStart = false;

            try
            {
                // Restore our temporary disable before OnInit so its enabled choice wins.
                if (restoreEnabled)
                {
                    enabled = true;
                }

                OnInit();

                if (PersistAcrossScenes)
                {
                    if (transform.parent != null)
                    {
                        throw new InvalidOperationException($"{GetType().Name} must remain on a root GameObject after OnInit to persist across scenes.");
                    }

                    DontDestroyOnLoad(gameObject);
                }
            }
            catch
            {
                if (restoreEnabled)
                {
                    _disabledForDeferredStart = true;
                    enabled = false;
                }

                throw;
            }
        }

        private void ResumeStart()
        {
            if (_startDeferred && !_isInitializing && !_isReleasing && this == _instance && isActiveAndEnabled)
            {
                _startDeferred = false;
                OnStart();
            }
        }

        /// <summary>
        /// Awake 또는 최초 Instance 접근 시 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 비활성 객체에서도 호출될 수 있으며, 성공적으로 완료된 후 인스턴스가 공개됩니다.
        /// 초기화 중에는 Instance와 ReleaseInstance를 호출할 수 없으므로 this를 사용합니다.
        /// 실패하면 이후 접근 시 재시도할 수 있습니다. 실패 시 OnRelease는 자동 호출되지 않으므로
        /// 초기화 도중 획득한 리소스는 직접 정리해야 합니다.
        /// </remarks>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// ReleaseInstance 호출 또는 현재 인스턴스의 파괴 시 리소스를 정리합니다.
        /// </summary>
        /// <remarks>
        /// 공개된 인스턴스 참조가 제거된 후 호출됩니다.
        /// 한 번도 활성화되지 않고 파괴된 객체는 다음 Instance 접근, 대체 객체의 Awake 또는
        /// 명시적 ReleaseInstance 호출 시 지연 정리됩니다. 이 경우 이미 파괴된 Unity 객체의
        /// 프로퍼티나 메서드에 접근하지 말고, 보관한 관리 리소스를 정리해야 합니다.
        /// 해제 중에는 Instance와 ReleaseInstance를 호출할 수 없으므로 this를 사용합니다.
        /// 예외가 발생해도 참조는 제거된 상태로 유지되며, 예외는 호출자에게 전파됩니다.
        /// </remarks>
        protected virtual void OnRelease()
        {
        }

        /// <summary>
        /// 초기화된 현재 인스턴스가 활성 상태일 때 시작 처리를 수행합니다.
        /// </summary>
        /// <remarks>
        /// 일반적으로 Unity의 Start에서 호출됩니다. Start 시점에 현재 인스턴스가 아니었다면
        /// 초기화에 성공하고 활성 상태가 된 후 호출됩니다.
        /// 보류된 호출은 한 번만 수행하며, 이후 재활성화나 재초기화만으로 반복 호출되지 않습니다.
        /// 이 시점에는 Instance에 접근할 수 있습니다.
        /// </remarks>
        protected virtual void OnStart()
        {
        }
    }

    /// <summary>
    /// SceneObject 계열의 Unity 생명주기 메서드를 정의하는 공통 기반 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스를 직접 상속하지 말고 <see cref="SceneObject{TComponent}"/>를 상속하세요.
    /// </remarks>
    public abstract class SceneObjectBase : MonoBehaviourCrowRx, IInstance
    {
        private protected SceneObjectBase()
        {
        }

        protected abstract void Awake();
        protected abstract void OnDestroy();
        protected abstract void OnEnable();
        protected abstract void Start();
    }
}