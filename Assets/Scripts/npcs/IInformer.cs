namespace DefaultNamespace.npcs
{
    public interface IInformer
    {
        string listInfo();
        string sellInfo(string name, int price);
        string buyInfo(int price);
    }
}