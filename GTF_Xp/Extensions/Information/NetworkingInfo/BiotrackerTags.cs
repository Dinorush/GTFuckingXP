using Enemies;

namespace GTFuckingXP.Information.NetworkingInfo
{
    public struct BiotrackerTags
    {
        
        public BiotrackerTags(List<EnemyAgent> taggedEnemies)
        {
            TaggedEnemies = taggedEnemies;
        }

        public List<EnemyAgent> TaggedEnemies { get; set; }
    }
}