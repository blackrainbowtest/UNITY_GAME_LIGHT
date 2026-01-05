public class GameState
{
    public static GameState Instance { get; private set; } = new GameState();
    public SaveData CurrentSave { get; set; }
    private GameState() { }
}
