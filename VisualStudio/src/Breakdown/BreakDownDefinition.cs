namespace SCPlus
{
    internal class BreakDownYield
    {
        public string item;
        public int num;
    }

    class BreakDownDefinition
    {
        public string filter = "";
        public string[] filters = [];
        public string sound = "";
        public float minutesToHarvest = 1f;

        public bool requireTool = false;
        public string[] tools = [];
        public BreakDownYield[] yield = [];

        public bool isSmallItem = false;

    }
}
