using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace UDA2.SceneFlow
{
public interface ILoadingScreen
{
void Show();
void Hide();
void SetProgress(float progress);
}

    public class SceneFlowManager : MonoBehaviour
{
public static SceneFlowManager Instance { get; private set; }

[Header("Audio Sync")]
[SerializeField] private bool waitForMusicBeforeHideLoading = true;
[SerializeField] private bool preloadSceneMusicBeforeActivation = true;
[SerializeField, Min(0f)] private float musicReadyTimeoutSeconds = 2f;
[SerializeField] private bool skipMusicWaitIfSceneHasNoMusic = true;
[SerializeField] private string[] skipMusicWaitForScenes = Array.Empty<string>();

[Header("Debug")]
[SerializeField] private bool logLoadTimings;

[Header("Scene Ready Sync")]
[Tooltip("If true, waits for NotifySceneReady signal from scene scripts.")]
[SerializeField] private bool waitForSceneReadySignal = true;
[Tooltip("Maximum wait for NotifySceneReady. After timeout, loader continues.")]
[SerializeField, Min(0f)] private float sceneReadyTimeoutSeconds = 1.5f;

private bool _sceneReady;
private ILoadingScreen loadingScreen;
private Coroutine _loadCoroutine;

private void Awake()
{
if (Instance != null)
{
Destroy(gameObject);
return;
}
Instance = this;
DontDestroyOnLoad(gameObject);
}

private void OnDisable()
{
if (_loadCoroutine != null)
{
StopCoroutine(_loadCoroutine);
_loadCoroutine = null;
}
}

private void OnDestroy()
{
if (Instance == this)
Instance = null;
}

public void RegisterLoadingScreen(ILoadingScreen screen)
{
loadingScreen = screen;
}

public void UnregisterLoadingScreen(ILoadingScreen screen)
{
if (loadingScreen == screen)
loadingScreen = null;
}

public void NotifySceneReady()
{
_sceneReady = true;
}


private const float DefaultMinLoadingTime = 1.0f; // по умолчанию 1 секунда


// мя вашей сцены загрузки
private const string LoadingSceneName = "LoadingScene";

// ерегрузка с минимальным временем загрузки
public void LoadScene(string sceneName, SceneTransitionData data = null, float? minLoadingTime = null)
{
float minTime = minLoadingTime ?? DefaultMinLoadingTime;
if (_loadCoroutine != null)
{
StopCoroutine(_loadCoroutine);
_loadCoroutine = null;
}
_loadCoroutine = StartCoroutine(LoadSceneWithLoadingScreen(sceneName, data, minTime));
}

// овый flow: всегда через LoadingScene
private IEnumerator LoadSceneWithLoadingScreen(string targetScene, SceneTransitionData data, float minLoadingTime)
{
// сли уже в LoadingScene, просто грузим целевую сцену
if (SceneManager.GetActiveScene().name == LoadingSceneName)
{
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
yield break;
}

// 1. агружаем LoadingScene
_sceneReady = false;
loadingScreen = null;
AsyncOperation loadingOp = SceneManager.LoadSceneAsync(LoadingSceneName);
while (!loadingOp.isDone)
yield return null;

// 2. дём, пока LoadingScreenController зарегистрируется
float waitTime = 0f;
while (loadingScreen == null && waitTime < 5f) // fail-safe 5 сек
{
waitTime += Time.unscaledDeltaTime;
yield return null;
}

// 3. оказываем loading (на всякий случай)
if (loadingScreen != null)
loadingScreen.Show();

// 4. рузим целевую сцену с задержкой
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
_loadCoroutine = null;
}

// бычная загрузка целевой сцены с ожиданием ready и минимального времени
private IEnumerator LoadSceneRoutine(string sceneName, SceneTransitionData data, float minLoadingTime)
{
float startedAt = Time.realtimeSinceStartup;
_sceneReady = false;

if (loadingScreen != null)
{
loadingScreen.Show();
loadingScreen.SetProgress(0f);
}

float timer = 0f;
TryPreloadSceneMusic(sceneName);
var asyncOp = SceneManager.LoadSceneAsync(sceneName);
if (asyncOp == null)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Failed to start async load for '{sceneName}'.");

if (loadingScreen != null)
loadingScreen.Hide();

yield break;
}

asyncOp.allowSceneActivation = false;
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Begin load '{sceneName}'. minLoadingTime={minLoadingTime:0.###}");

while (asyncOp.progress < 0.9f)
{
timer += Time.unscaledDeltaTime;
if (loadingScreen != null)
{
float streamProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
loadingScreen.SetProgress(Mathf.Lerp(0f, 0.9f, streamProgress));
}

yield return null;
}

if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Async load finished for '{sceneName}' at {timer:0.###}s");

float normalizedMin = minLoadingTime > 0f ? Mathf.Clamp01(timer / minLoadingTime) : 1f;
while (normalizedMin < 1f)
{
timer += Time.unscaledDeltaTime;
normalizedMin = minLoadingTime > 0f ? Mathf.Clamp01(timer / minLoadingTime) : 1f;

if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Lerp(0.9f, 0.95f, normalizedMin));

yield return null;
}

if (loadingScreen != null)
loadingScreen.SetProgress(0.95f);

asyncOp.allowSceneActivation = true;
while (!asyncOp.isDone)
{
timer += Time.unscaledDeltaTime;

if (loadingScreen != null)
loadingScreen.SetProgress(0.97f);

yield return null;
}

bool shouldWaitForSceneReady = waitForSceneReadySignal && SceneHasReadySignalReceiver();

if (shouldWaitForSceneReady)
{
float sceneReadyWaited = 0f;
float sceneReadyTimeout = Mathf.Max(0f, sceneReadyTimeoutSeconds);

while (!_sceneReady && sceneReadyWaited < sceneReadyTimeout)
{
sceneReadyWaited += Time.unscaledDeltaTime;
timer += Time.unscaledDeltaTime;

if (loadingScreen != null)
loadingScreen.SetProgress(0.98f);

yield return null;
}

if (logLoadTimings)
{
if (_sceneReady)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Scene ready signaled for '{sceneName}' after {timer:0.###}s (extraWait={sceneReadyWaited:0.###}s)");
else
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Scene ready timeout for '{sceneName}' after {sceneReadyWaited:0.###}s. Continuing load.");
}
}
else if (logLoadTimings)
{
if (!waitForSceneReadySignal)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Scene ready signal disabled for '{sceneName}'. Continuing after async load.");
else
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Scene ready wait skipped for '{sceneName}' (no ISceneReady receiver in scene).");
}

if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Min loading time reached for '{sceneName}' at {timer:0.###}s");

if (loadingScreen != null)
loadingScreen.SetProgress(0.99f);

yield return WaitForMusicReady(sceneName);

if (logLoadTimings)
{
float total = Time.realtimeSinceStartup - startedAt;
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Hide loading for '{sceneName}'. total={total:0.###}s");
}

if (loadingScreen != null)
{
loadingScreen.SetProgress(1f);
loadingScreen.Hide();
}
}

private IEnumerator WaitForMusicReady(string sceneName)
{
if (!waitForMusicBeforeHideLoading)
yield break;

if (ShouldSkipMusicWait(sceneName))
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Skip music wait for scene '{sceneName}' (configured override)");
yield break;
}

var timeout = Mathf.Max(0f, musicReadyTimeoutSeconds);
if (timeout <= 0f)
yield break;

float waited = 0f;
while (!TryGetAudioManagerSingleton(out var audio) && waited < timeout)
{
waited += Time.unscaledDeltaTime;
yield return null;
}

if (!TryGetAudioManagerSingleton(out var audioManager))
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Skip music wait for '{sceneName}' (AudioManager not found)");
yield break;
}

if (skipMusicWaitIfSceneHasNoMusic && !WillPlayMusicForScene(audioManager, sceneName))
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Skip music wait for '{sceneName}' (no music configured)");
yield break;
}

waited = 0f;
while (!IsMusicPlaying(audioManager) && waited < timeout)
{
waited += Time.unscaledDeltaTime;
yield return null;
}

if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Music wait for '{sceneName}' finished. waited={waited:0.###}s timeout={timeout:0.###}s");
}

private bool ShouldSkipMusicWait(string sceneName)
{
if (skipMusicWaitForScenes == null || skipMusicWaitForScenes.Length == 0)
return false;
for (int i = 0; i < skipMusicWaitForScenes.Length; i++)
{
var item = skipMusicWaitForScenes[i];
if (string.IsNullOrWhiteSpace(item))
continue;

if (string.Equals(item.Trim(), sceneName, StringComparison.Ordinal))
return true;
}

return false;
}

private void TryPreloadSceneMusic(string sceneName)
{
if (!preloadSceneMusicBeforeActivation)
return;

if (string.IsNullOrWhiteSpace(sceneName))
return;

if (!TryGetAudioManagerSingleton(out var audioManager))
return;

var method = audioManager.GetType().GetMethod(
"PreloadSceneMusicAudioData",
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
null,
new[] { typeof(string) },
null);

if (method == null)
return;

try
{
method.Invoke(audioManager, new object[] { sceneName });
}
catch (Exception)
{
// Ignore prewarm failures to keep scene transitions robust.
}
}

private static bool TryGetAudioManagerSingleton(out object audioManager)
{
audioManager = null;

var type = System.Type.GetType("UDA2.Audio.AudioManager, UDA2.Audio");
if (type == null)
return false;

var instanceField = type.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
if (instanceField == null)
return false;

audioManager = instanceField.GetValue(null);
return audioManager != null;
}

private static bool IsMusicPlaying(object audioManager)
{
if (audioManager == null)
return false;

var property = audioManager.GetType().GetProperty("IsMusicPlaying", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
if (property == null || property.PropertyType != typeof(bool))
return false;

var value = property.GetValue(audioManager);
return value is bool isPlaying && isPlaying;
}

private static bool WillPlayMusicForScene(object audioManager, string sceneName)
{
if (audioManager == null)
return false;

var method = audioManager.GetType().GetMethod(
"WillPlayMusicForScene",
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
null,
new[] { typeof(string) },
null);

if (method == null)
return false;

var result = method.Invoke(audioManager, new object[] { sceneName });
return result is bool hasMusic && hasMusic;
}

private static bool SceneHasReadySignalReceiver()
{
var scene = SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded)
return false;

var roots = scene.GetRootGameObjects();
for (int i = 0; i < roots.Length; i++)
{
var root = roots[i];
if (root == null)
continue;

var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
for (int j = 0; j < behaviours.Length; j++)
{
if (behaviours[j] is ISceneReady)
return true;
}
}

return false;
}
}
}
