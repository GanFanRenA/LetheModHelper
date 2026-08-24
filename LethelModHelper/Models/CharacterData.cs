namespace LethelModHelper.Models
{
    public class CharacterData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }

        public override string ToString()
        {
            return $"{Id} - {Name} (Lv.{Level})";
        }
    }
}