namespace CrowRx.Singleton
{
    /// <summary>
    /// SceneObject의 동작에 씬 전환 시 GameObject를 유지하는 기능을 추가합니다.
    /// </summary>
    /// <typeparam name="TComponent">자기 자신을 타입 인수로 지정하는 파생 컴포넌트 타입입니다.</typeparam>
    /// <remarks>
    /// 씬의 루트 GameObject에 배치해야 하며, 객체를 자동 생성하지 않습니다.
    /// Awake 또는 Instance 접근으로 초기화에 성공하면 DontDestroyOnLoad를 적용합니다.
    /// 선택된 GameObject와 그 자식 객체가 함께 유지됩니다.
    /// </remarks>
    public abstract class StaticSceneObject<TComponent> : SceneObject<TComponent>
        where TComponent : StaticSceneObject<TComponent>
    {
        private protected sealed override bool PersistAcrossScenes => true;
    }
}