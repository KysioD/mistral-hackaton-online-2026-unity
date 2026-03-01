namespace DefaultNamespace.npcs
{
    public interface ITrader
    {
        string ListPlayerItems();
        string BuyItem(string name, int price);
    }
}