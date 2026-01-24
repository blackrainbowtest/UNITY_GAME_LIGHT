using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UIStringsProvider : MonoBehaviour
{
    public static UIStringsProvider Instance { get; private set; }

    [SerializeField]
    private UIStringsData[] dataAssets;

    [SerializeField]
    private string defaultLanguage = "en";

    private string currentLanguage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple UIStringsProvider instances detected. Only one is allowed.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        currentLanguage = defaultLanguage;
    }

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

    public void SetLanguage(string lang)
    {
        currentLanguage = lang;
    }

    public string Get(string key)
    {
        return Get(key, currentLanguage);
    }

    public string Get(string key, string lang)
    {
        foreach (var data in dataAssets)
        {
            if (data == null) continue;
            var result = data.Get(key, lang);
            if (!string.IsNullOrEmpty(result) && result != key)
                return result;
        }
        return key; // fallback: return key if not found
    }

    public string CurrentLanguage => currentLanguage;
}
