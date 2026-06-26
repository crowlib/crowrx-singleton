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

        protected abstract void OnInit();
        protected abstract void OnRelease();
    }
}