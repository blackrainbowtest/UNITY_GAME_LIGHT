using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CityStubPanel : MonoBehaviour
{
    [Header("Scene to Load")]
    public string targetSceneName;
    public Button goToCityButton;

    void Awake()
    {
        if (goToCityButton != null)
            goToCityButton.onClick.AddListener(OnGoToCityClicked);
    }

    void OnGoToCityClicked()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            var loader = UDA2.SceneFlow.SceneFlowManager.Instance;
            if (loader != null)
            {
                loader.LoadScene(targetSceneName);
                return;
            }

            Debug.LogWarning(
                $"[{nameof(CityStubPanel)}] SceneFlowManager.Instance is null. Falling back to SceneManager.LoadSceneAsync('{targetSceneName}')",
                this);
            SceneManager.LoadSceneAsync(targetSceneName);
        }
    }
}
