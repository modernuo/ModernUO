namespace Server
{
    public delegate MoveResult MoveMethod(Direction d, bool badStateOk);

    public enum MoveResult
    {
        BadState,
        Blocked,
        Success,
        SuccessAutoTurn
    }
}
