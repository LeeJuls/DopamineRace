/// <summary>
/// SFX 키 상수 모음 — 사운드 키 문자열이 존재하는 유일한 곳.
/// 호출부는 반드시 이 상수를 참조("sfx.xxx" 리터럴 직접 사용 금지, 오타 시 컴파일 에러로 즉시 차단).
/// 파일 경로/AudioClip은 여기 없음 — SFXSettings.asset에서만 관리(하드코딩 금지 원칙).
/// </summary>
public static class SFXKeys
{
    public const string RaceCollisionHit = "sfx.race.collision.hit";
    public const string RaceCollisionDodge = "sfx.race.collision.dodge";
    public const string RaceCollisionSlingshot = "sfx.race.collision.slingshot";
    public const string RaceBuffCrit = "sfx.race.buff.crit";
    public const string RaceBuffClutch = "sfx.race.buff.clutch";
    public const string RaceBuffActive = "sfx.race.buff.active";
    public const string RaceRun = "sfx.race.run";
    public const string JackpotRoll = "sfx.jackpot.roll";
    public const string JackpotReveal = "sfx.jackpot.reveal";
    public const string JackpotOpen = "sfx.jackpot.open";
    public const string RaceStart = "sfx.race.start";
    public const string RaceCountdown = "sfx.race.countdown";
}
