namespace GTFuckingXP.Information.NetworkingInfo
{
    public struct InitXpInfo
    {
        public InitXpInfo(uint xp, uint checkpointXp)
        {
            Xp = xp;
            CheckpointXp = checkpointXp;
        }

        public uint Xp { get; set; }
        public uint CheckpointXp { get; set; }
    }
}
