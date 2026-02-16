public sealed class SimplePhaseProfileResolver : IPhaseProfileResolver
{
    public string ResolveNextProfileKey(string currentProfileKey, PhaseDecisionKind decisionKind)
    {
        switch (decisionKind)
        {
            case PhaseDecisionKind.BuildTrust:  return "TalkWarm";
            case PhaseDecisionKind.PushSupport: return "DonationDrive";
            case PhaseDecisionKind.ManageRisk:  return "Crisis";
            case PhaseDecisionKind.StirHeat:    return "Hype";
            default:                            return currentProfileKey;
        }
    }
}
