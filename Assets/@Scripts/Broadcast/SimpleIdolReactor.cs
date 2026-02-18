public sealed class SimpleIdolReactor
{
    public IdolReaction React(ChatTag tag, string optionId)
    {
        // P0 예시: 태그 기반의 단순 매핑
        // - Analysis: 긍정
        // - Instinct: 중립
        // - Chaos: 부정
        // optionId 기반 룰은 P1에서 확장
        _ = optionId;

        switch (tag)
        {
            case ChatTag.Analysis:  return IdolReaction.Positive;
            case ChatTag.Instinct:  return IdolReaction.Neutral;
            case ChatTag.Chaos:     return IdolReaction.Negative;
            default:                return IdolReaction.Neutral;
        }
    }
}