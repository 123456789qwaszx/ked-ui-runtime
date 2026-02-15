using System;

[Flags]
public enum ChatSignalFlags : uint
{
    None = 0,
    IdolSpoke = 1 << 0,
    ISpoke = 1 << 1,
    DonationHappened = 1 << 2,
    BigDonationHappened = 1 << 3,
    SystemNotice = 1 << 4,
}

public readonly struct ChatSignals
{
    public readonly ChatSignalFlags flags;

    // 선택: 최근 후원 금액 같은 “순간 트리거 데이터”
    public readonly int donationAmount;

    public ChatSignals(ChatSignalFlags flags, int donationAmount = 0)
    {
        this.flags = flags;
        this.donationAmount = donationAmount;
    }

    public bool Has(ChatSignalFlags f) => (flags & f) != 0;
}