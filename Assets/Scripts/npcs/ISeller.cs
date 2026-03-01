namespace DefaultNamespace.npcs
{
    public interface ISeller
    {
        string giveItem(string name);
        string sellItem(string name, int price);
        string listItems();
    }
}