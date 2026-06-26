using System;

namespace CrowRx.Singleton
{
    public abstract class Native<TInstance> : IInstance
        where TInstance : Native<TInstance>
    {
        private static TInstance? _instance;

        public static TInstance Instance
        {
            get
            {
                if (_instance is not null)
                {
                    return _instance;
                }

                _instance = Activator.CreateInstance<TInstance>();
                _instance.Init();

                return _instance;
            }
        }

        public static bool IsValid => _instance is not null;

        public void Init() => OnInit();

        public void Release()
        {
            OnRelease();

            _instance = null;
        }

        /// <summary>
        /// 초기화 시점에서 사용됩니다. Instance가 유효한 상태에서 호출됩니다.
        /// 직접 호출은 권장하지 않습니다. 
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 소멸 시점에서 사용됩니다. Instance가 유효한 상태에서 호출됩니다. 이후 Instance는 유효하지 않습니다.
        /// 직접 호출은 권장하지 않습니다. 
        /// </summary>
        protected virtual void OnRelease() { }
    }
}