namespace npcs.dto
{
    [System.Serializable]
    public class InfoDto
    {
        public string name;
        public string quickDescription;
        public string content;
        public int price;

        public override string ToString()
        {
            return $"{name} - {quickDescription}\n Info : {content}\nPrice: {price} gold";
        }
    }
}