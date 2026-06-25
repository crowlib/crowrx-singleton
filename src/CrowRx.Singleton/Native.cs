using System;


namespace CrowRx.Singleton
{
    public class Native<TInstance>
        where TInstance : Native<TInstance>, IInstance
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
    }
}