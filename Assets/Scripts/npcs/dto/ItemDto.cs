namespace npcs.dto
{
    public class ItemDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Description} - {Price} gold";
        }
    }
}