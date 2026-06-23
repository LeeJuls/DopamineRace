/// <summary>
/// 메모리 난독화 정수 — Cheat Engine류 평문 스캔 지연 (SPEC-044 Phase C/D).
/// value를 직접 저장하지 않고 서로 다른 마스크로 XOR한 사본 2개로 보관.
/// 읽을 때 두 사본을 복호해 일치 검사 → 백킹필드 단독 변조 탐지.
/// 이중 사본이라 default(SecureInt)(전부 0)도 value=0으로 정상 복호(오탐 0).
/// ※ 난독화이지 암호화 아님 — 캐주얼 치터 지연용. 시그니처 호환 위해 int 암묵 변환 제공.
/// </summary>
public struct SecureInt
{
    private int _maskA, _encA;   // value ^ _maskA
    private int _maskB, _encB;   // value ^ _maskB

    // 공유 정적 RNG (마스크 예측 불가용). 게임 로직은 단일 스레드라 동기화 불필요.
    private static readonly System.Random _rng = new System.Random();

    public SecureInt(int value)
    {
        _maskA = _rng.Next(); _encA = value ^ _maskA;
        _maskB = _rng.Next(); _encB = value ^ _maskB;
    }

    public int Value
    {
        get
        {
            int a = _encA ^ _maskA;
            int b = _encB ^ _maskB;
            if (a != b)
            {
                UnityEngine.Debug.LogWarning("[SecureInt] 메모리 변조 감지 — 0으로 폴백");
                return 0;
            }
            return a;
        }
        set
        {
            _maskA = _rng.Next(); _encA = value ^ _maskA;
            _maskB = _rng.Next(); _encB = value ^ _maskB;
        }
    }

    // int 드롭인 — 호출부(_stone -= cost, _jelly >= amount 등) 무수정.
    public static implicit operator int(SecureInt s) => s.Value;
    public static implicit operator SecureInt(int v) => new SecureInt(v);

    public override string ToString() => Value.ToString();
}
