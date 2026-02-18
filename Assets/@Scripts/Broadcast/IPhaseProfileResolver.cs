public interface IPhaseProfileResolver
{
    string ResolveNextProfileKey(string currentProfileKey, PhaseDecisionKind decisionKind);
}