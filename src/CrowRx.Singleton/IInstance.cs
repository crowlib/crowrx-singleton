namespace CrowRx.Singleton
{
    public interface IInstance
    {
        /// <summary>
        /// Instance 초기화를 위해 사용됩니다.
        /// 직접 호출은 권장하지 않습니다. 
        /// </summary>
        void Init();

        /// <summary>
        /// Instance 소멸을 위해 사용됩니다.
        /// 직접 호출은 권장하지 않습니다. 
        /// </summary>
        void Release();
    }
}