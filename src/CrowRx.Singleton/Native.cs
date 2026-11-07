using System;
using System.Threading;

namespace CrowRx.Singleton
{
    public abstract class Native<TInstance> : IInstance
        where TInstance : Native<TInstance>
    {
        private static readonly object _syncRoot = new();
        private static TInstance? _instance;
        private static bool _isInitializing;
        private static bool _isReleasing;

        public static TInstance Instance
        {
            get
            {
                TInstance? instance = Volatile.Read(ref _instance);
                if (instance is not null)
                {
                    return instance;
                }

                lock (_syncRoot)
                {
                    instance = _instance;
                    if (instance is not null)
                    {
                        return instance;
                    }

                    if (_isInitializing || _isReleasing)
                    {
                        throw new InvalidOperationException($"Cannot access {typeof(TInstance).Name}.Instance during initialization or release.");
                    }

                    _isInitializing = true;

                    try
                    {
                        instance = (TInstance?)Activator.CreateInstance(typeof(TInstance), nonPublic: true) ?? throw new InvalidOperationException($"Failed to create an instance of {typeof(TInstance).FullName}.");
                        instance.OnInit();

                        Volatile.Write(ref _instance, instance);

                        return instance;
                    }
                    finally
                    {
                        _isInitializing = false;
                    }
                }
            }
        }

        /// <summary>
        /// 다른 스레드에서 인스턴스 사용이 끝난 이후 호출해야 합니다.
        /// </summary>
        public static void ReleaseInstance()
        {
            lock (_syncRoot)
            {
                if (_isInitializing || _isReleasing)
                {
                    throw new InvalidOperationException($"Cannot access {typeof(TInstance).Name}.ReleaseInstance during initialization or release.");
                }

                TInstance? instance = _instance;
                if (instance is null)
                {
                    return;
                }

                _isReleasing = true;

                Volatile.Write(ref _instance, null);

                try
                {
                    instance.OnRelease();
                }
                finally
                {
                    _isReleasing = false;
                }
            }
        }

        public static bool IsValid => Volatile.Read(ref _instance) is not null;

        /// <summary>
        /// 초기화 시점에서 사용됩니다. 초기화가 완료된 이후 Instance가 공개됩니다.
        /// 초기화 중에는 Instance 대신 this를 사용해야 합니다.
        /// 직접 호출은 권장하지 않으며, 신중하게 해야 합니다.
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 소멸 시점에서 사용됩니다. Instance 공개를 해제한 이후 호출됩니다.
        /// 해제 중에는 Instance 대신 this를 사용해야 합니다.
        /// 직접 호출은 권장하지 않으며, 신중하게 해야 합니다.
        /// </summary>
        protected virtual void OnRelease() { }
    }
}
