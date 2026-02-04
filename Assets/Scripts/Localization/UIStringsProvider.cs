using UnityEngine;
using System;
using System.Globalization;

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

    /// <summary>
    /// Gets a localized template and applies <see cref="string.Format(string,object[])"/>.
    /// Use numbered placeholders like "Lv {0} • Gold {1}".
    /// If formatting fails, returns the template as-is.
    /// </summary>
    public string GetFormatted(string key, string lang, params object[] args)
    {
        var template = Get(key, lang);
        if (args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string GetFormatted(string key, params object[] args)
    {
        return GetFormatted(key, currentLanguage, args);
    }

    public string CurrentLanguage => currentLanguage;
}
