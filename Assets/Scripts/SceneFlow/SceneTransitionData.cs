namespace UDA2.SceneFlow
{
    // Контейнер для параметров перехода между сценами
    public class SceneTransitionData
    {
        // Пример параметров, расширяйте по необходимости
        public string PlayerName;
        public int LevelToLoad;

        // Fast transition flags (useful for battle entry where post-load waits are unnecessary).
        public bool SkipSceneLoadTasks;
        public bool SkipSceneReadyWait;
        public bool SkipMusicWait;
        public bool DisableFakeProgressEnvelope;
    }
}
