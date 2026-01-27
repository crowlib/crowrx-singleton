namespace CrowRx.Singleton
{
    public interface IInstance
    {
        void Init();
        void Release();
    }
}