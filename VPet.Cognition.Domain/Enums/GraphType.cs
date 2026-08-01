namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// Animation graph type. Mirrors the <c>VPet_Simulator.Core.GraphInfo.GraphType</c> enum so the
/// Domain layer can express animation hints without depending on Core. The cognition host maps
/// between this and the Core enum at the boundary.
/// </summary>
public enum GraphType
{
    Common = 0,
    Raised_Dynamic = 1,
    Raised_Static = 2,
    Move = 3,
    Default = 4,
    Touch_Head = 5,
    Touch_Body = 6,
    Idel = 7,
    Sleep = 8,
    Say = 9,
    StateONE = 10,
    StateTWO = 11,
    StartUP = 12,
    Shutdown = 13,
    Work = 14,
    Switch_Up = 15,
    Switch_Down = 16,
    Switch_Thirsty = 17,
    Switch_Hunger = 18,
    SideHide_Left_Main = 19,
    SideHide_Left_Rise = 20,
    SideHide_Right_Main = 21,
    SideHide_Right_Rise = 22,
}

/// <summary>
/// Animation mood mode. Mirrors <c>VPet_Simulator.Core.IGameSave.ModeType</c>, ordered 0..3
/// so the memory store's fallback logic (exact → +1 → −1 → any non-Ill) is preserved.
/// </summary>
public enum ModeType
{
    Happy = 0,
    Nomal = 1,
    PoorCondition = 2,
    Ill = 3,
}

/// <summary>
/// Animation phase. Mirrors <c>VPet_Simulator.Core.GraphInfo.AnimatType</c>.
/// </summary>
public enum AnimatType
{
    Single = 0,
    A_Start = 1,
    B_Loop = 2,
    C_End = 3,
}