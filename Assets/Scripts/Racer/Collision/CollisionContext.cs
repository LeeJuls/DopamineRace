/// <summary>
/// 충돌 이벤트 컨텍스트 — RacerController.OnSkillCollisionHit에 방향 정보 전달.
/// bool 파라미터 대신 구조체로 확장성 확보 (향후 relSpeed/hitPoint 등 추가 가능).
/// </summary>
public struct CollisionContext
{
    /// <summary>true = 뒤→앞을 타격하는 추격자, false = 앞에서 뒤에게 피격당하는 쪽</summary>
    public bool isChasing;
}
