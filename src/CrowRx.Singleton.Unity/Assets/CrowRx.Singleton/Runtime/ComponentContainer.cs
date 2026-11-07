using System;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace CrowRx.Singleton
{
    using Object = UnityEngine.Object;

    /// <summary>
    /// 순수 C# 클래스에서도 씬의 기존 컴포넌트에 타입별 단일 참조로 접근할 수 있게 합니다.
    /// </summary>
    /// <typeparam name="TComponent">참조할 컴포넌트 타입입니다. IInstance 구현은 필요하지 않습니다.</typeparam>
    /// <remarks>
    /// Unity 메인 스레드에서 사용합니다. 비활성 객체를 포함하여 컴포넌트를 검색하고 참조를 캐시합니다.
    /// 컴포넌트를 생성하거나 초기화, 활성화, 파괴하지 않으며 생명주기는 해당 컴포넌트가 관리합니다.
    /// 캐시된 컴포넌트가 파괴되면 다음 Instance 접근 시 다시 검색합니다.
    /// 여러 컴포넌트가 검색되면 Behaviour는 isActiveAndEnabled인 컴포넌트를,
    /// 그 외 컴포넌트는 GameObject가 activeInHierarchy인 컴포넌트를 우선 선택합니다.
    /// 우선 선택할 컴포넌트가 여러 개이면 오류를 기록하며, 하나도 없으면 검색 결과의 첫 번째를 선택합니다.
    /// </remarks>
    public abstract partial class ComponentContainer<TComponent> : IInstance
        where TComponent : Component
    {
        [AutoStaticsCleanup] private static TComponent _instance;

        public static TComponent Instance
        {
            get
            {
                if (_instance)
                {
                    return _instance;
                }

                _instance = null;

                TComponent[] objects = Object.FindObjectsByType<TComponent>(FindObjectsInactive.Include);
                if (objects == null || objects.Length == 0)
                {
                    throw new InvalidOperationException($"There is no <{typeof(TComponent).Name}> in the scene objects.");
                }

                TComponent instance = null;
                if (objects.Length > 1)
                {
                    int availableComponentCount = 0;
                    foreach (TComponent component in objects)
                    {
                        bool isAvailable = component is Behaviour behaviour
                            ? behaviour.isActiveAndEnabled
                            : component.gameObject.activeInHierarchy;

                        if (isAvailable)
                        {
                            ++availableComponentCount;
                            instance ??= component;
                        }
                    }

                    if (availableComponentCount > 1)
                    {
                        Log.Error($"[ComponentContainer<{typeof(TComponent).Name}>] More than one available component was found. Using the first available component.");
                    }
                }

                _instance = instance ?? objects[0];

                return _instance;
            }
        }

        public static bool IsValid => _instance;
    }
}