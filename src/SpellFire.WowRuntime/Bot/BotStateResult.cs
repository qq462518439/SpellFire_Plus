namespace SpellFire.WowRuntime.Bot
{
    public sealed class BotStateResult
    {
        public BotStateResult(bool ran, string stateName, string detail)
        {
            Ran = ran;
            StateName = stateName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool Ran { get; }

        public string StateName { get; }

        public string Detail { get; }
    }
}
