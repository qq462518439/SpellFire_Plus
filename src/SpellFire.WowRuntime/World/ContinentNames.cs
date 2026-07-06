namespace SpellFire.WowRuntime.World
{
    public static class ContinentNames
    {
        public static string GetName(int continentId)
        {
            switch (continentId)
            {
                case 0:
                    return "Azeroth";
                case 1:
                    return "Kalimdor";
                case 530:
                    return "Expansion01";
                case 571:
                    return "Northrend";
                case 607:
                    return "NorthrendBG";
                case 609:
                    return "DeathKnightStart";
                case 631:
                    return "IcecrownCitadel";
                default:
                    return string.Empty;
            }
        }
    }
}
