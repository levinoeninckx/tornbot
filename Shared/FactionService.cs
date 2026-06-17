namespace TornBot.Bot.Shared;

public class FactionService
{
    private int _factionId;
    
    public int FactionId() => _factionId;
    public void SetFactionId(int factionId)
    {
        _factionId = factionId;
    }
}