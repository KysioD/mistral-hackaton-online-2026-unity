namespace DefaultNamespace.npcs
{
    public interface IInformer
    {
        string listInfo();
        string sellInfo(int price);
        string buyInfo(int price);
    }
}