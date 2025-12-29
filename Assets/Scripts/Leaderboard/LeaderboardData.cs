using System;
using System.Collections.Generic;

[Serializable]
public class PlayerLeaderboardEntry
{
    public string nickname;
    public int kills;
    public int deaths;
    public int gamesPlayed;
}

[Serializable]
public class LeaderboardResponse
{
    public List<PlayerLeaderboardEntry> leaderboard;
    public int totalPlayers;
}

[Serializable]
public class PlayerStatsResponse
{
    public string nickname;
    public int kills;
    public int deaths;
    public int gamesPlayed;
}