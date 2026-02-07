public interface IBirdState
{
    void EnterState(BirdManager enemy);
    void UpdateState(BirdManager enemy);
    void ExitState(BirdManager enemy);
}