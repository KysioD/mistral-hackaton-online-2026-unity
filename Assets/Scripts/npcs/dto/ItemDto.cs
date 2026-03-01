namespace npcs.dto
{
    [System.Serializable]
    public class ItemDto
    {
        public string Name;
        public string Description;
        public int Price;

        public override string ToString()
        {
            return $"{Name} - {Description} - {Price} gold";
        }
    }
}