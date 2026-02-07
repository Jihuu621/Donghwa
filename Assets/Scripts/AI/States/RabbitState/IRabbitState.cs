public interface IRabbitState
{
    void EnterState(RabbitManager enemy);
    void UpdateState(RabbitManager enemy);
    void ExitState(RabbitManager enemy);
}