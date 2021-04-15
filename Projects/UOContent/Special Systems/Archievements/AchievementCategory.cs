namespace Scripts.Systems.Achievements
{
    public class AchievementCategory
    {
        public int ID { get; set; }
        public int Parent { get; set; }
        public string Name;


        public AchievementCategory(int id, int parent, string v3)
        {
            ID = id;
            Parent = parent;
            Name = v3;
        }
    }
}
