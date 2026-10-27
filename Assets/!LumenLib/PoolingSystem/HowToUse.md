Pooling System 사용법

1. Hierarchy에 빈 GameObject를 만들고 이름을 ObjectPool로 지정합니다.
2. ObjectPool 스크립트를 추가합니다.
3. Inspector의 Pool List에 사용할 PoolingListSO 에셋을 연결합니다.
4. GameObject를 활성화한 상태에서 실행합니다.
5. PoolingListSO에 있는 PoolItem들이 Pooling System을 사용합니다.

Pooling Item 만들기
1. IPoolable을 구현한 클래스를 만듭니다.
2. 해당 스크립트를 루트 오브젝트에 붙여줍니다.
3. Pool Item(SO)을 만들어서 이름을 중복되지 않게 지정합니다.
4. 프리팹과 초기 생성 갯수를 설정합니다.

사용 예시
using LumenLib.PoolingSystem.Runtime;
using Services = LumenLib.ServiceLocator.ServiceLocator;

if (Services.TryGet<ObjectPool>(out var pool))
{
    IPoolable bullet = pool.Pop("Bullet");
    if (bullet != null)
    {
        bullet.gameObject.transform.position = transform.position;
    }
}