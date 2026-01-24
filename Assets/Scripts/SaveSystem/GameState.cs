using UnityEngine;

public class GameState
{
    public static GameState Instance { get; private set; } = new GameState();
    public SaveData CurrentSave { get; set; }
    private GameState() { }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		Instance = new GameState();
	}
}
