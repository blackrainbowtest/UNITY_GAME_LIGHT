using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Collections.Generic;

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
public static bool IsTransitionInProgress { get; private set; }
public static event Action<bool> TransitionStateChanged;

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

[Header("Staged Scene Tasks")]
[SerializeField] private bool runSceneLoadTasks = true;
[SerializeField, Min(0f)] private float maxSingleLoadTaskSeconds = 1.5f;
[SerializeField, Range(0f, 1f)] private float loadProgressStart = 0.03f;
[SerializeField, Range(0f, 1f)] private float loadProgressStreamEnd = 0.90f;
[SerializeField, Range(0f, 1f)] private float loadProgressMinTimeEnd = 0.94f;
[SerializeField, Range(0f, 1f)] private float loadProgressActivationEnd = 0.96f;
[SerializeField, Range(0f, 1f)] private float loadProgressTasksEnd = 0.985f;
[SerializeField, Range(0f, 1f)] private float loadProgressSceneReadyEnd = 0.99f;
[SerializeField, Range(0f, 1f)] private float loadProgressMusicEnd = 0.995f;
[SerializeField, Min(0.01f)] private float runningTaskProgressSpeed = 0.6f;

[Header("Fake Progress Envelope")]
[SerializeField] private bool useFakeProgressEnvelope = true;
[SerializeField, Range(0.10f, 0.15f)] private float fakePreloadEndMin = 0.10f;
[SerializeField, Range(0.10f, 0.15f)] private float fakePreloadEndMax = 0.15f;
[SerializeField, Min(0f)] private float fakePreloadDurationMin = 0.2f;
[SerializeField, Min(0f)] private float fakePreloadDurationMax = 0.45f;
[SerializeField, Range(0.90f, 0.95f)] private float fakeFinalizeStartMin = 0.90f;
[SerializeField, Range(0.90f, 0.95f)] private float fakeFinalizeStartMax = 0.95f;
[SerializeField, Min(0f)] private float fakeFinalizeDurationMin = 0.2f;
[SerializeField, Min(0f)] private float fakeFinalizeDurationMax = 0.45f;

[Header("Loading Screen Resolve")]
[SerializeField, Min(0f)] private float loadingScreenResolveTimeoutSeconds = 0.75f;

[Header("Deferred Localization")]
[SerializeField] private bool drainDeferredLocalizationAfterActivation = true;
[SerializeField, Min(1)] private int maxDeferredLocalizationPerFrame = 32;
[SerializeField, Min(0f)] private float deferredLocalizationDrainTimeoutSeconds = 0.12f;

private bool _sceneReady;
private ILoadingScreen loadingScreen;
private Coroutine _loadCoroutine;
private float _progressCeilingBeforeFinalize = 1f;

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

SetTransitionInProgress(false);
}

private void OnDestroy()
{
if (Instance == this)
Instance = null;

SetTransitionInProgress(false);
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


private const float DefaultMinLoadingTime = 1.0f; // Default minimum loading duration.


// Name of the dedicated loading scene.
private const string LoadingSceneName = "LoadingScene";

// Public API: load a scene with optional transition data and minimum loading time.
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

// Transition flow: always route through LoadingScene.
private IEnumerator LoadSceneWithLoadingScreen(string targetScene, SceneTransitionData data, float minLoadingTime)
{
try
{
// If we are already in LoadingScene, continue directly with the target load routine.
if (SceneManager.GetActiveScene().name == LoadingSceneName)
{
SetTransitionInProgress(true);
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
yield break;
}

// 1) Load LoadingScene first.
_sceneReady = false;
loadingScreen = null;
AsyncOperation loadingOp = SceneManager.LoadSceneAsync(LoadingSceneName);
if (loadingOp == null)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Failed to load '{LoadingSceneName}'. Falling back to direct load for '{targetScene}'.");

SetTransitionInProgress(true);
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
yield break;
}

while (!loadingOp.isDone)
yield return null;

// 2) Wait for LoadingScreenController registration.
float waitTime = 0f;
float resolveTimeout = Mathf.Max(0f, loadingScreenResolveTimeoutSeconds);
while (loadingScreen == null && waitTime < resolveTimeout)
{
TryResolveLoadingScreenFromActiveScene();

if (loadingScreen != null)
break;

waitTime += Time.unscaledDeltaTime;
yield return null;
}

if (loadingScreen == null)
TryResolveLoadingScreenFromActiveScene();

// Hide global UI only when loader scene is active (or loader is already available),
// so player never sees a blank gap before loading visuals appear.
SetTransitionInProgress(true);

// 3) Load the target scene with staged progress and waits.
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
}
finally
{
_loadCoroutine = null;
SetTransitionInProgress(false);
}
}

private static void SetTransitionInProgress(bool isInProgress)
{
if (IsTransitionInProgress == isInProgress)
return;

IsTransitionInProgress = isInProgress;

try
{
TransitionStateChanged?.Invoke(isInProgress);
}
catch (Exception)
{
}
}

// Core target scene loading routine with staged waits and optional synchronization gates.
private IEnumerator LoadSceneRoutine(string sceneName, SceneTransitionData data, float minLoadingTime)
{
float startedAt = Time.realtimeSinceStartup;
_sceneReady = false;
bool useFakeProgressForTransition = useFakeProgressEnvelope && !(data != null && data.DisableFakeProgressEnvelope);

float progressCeiling = loadProgressMusicEnd;
if (loadingScreen != null && useFakeProgressForTransition)
{
float fakeStartMin = Mathf.Min(fakeFinalizeStartMin, fakeFinalizeStartMax);
float fakeStartMax = Mathf.Max(fakeFinalizeStartMin, fakeFinalizeStartMax);
progressCeiling = UnityEngine.Random.Range(fakeStartMin, fakeStartMax);
}

progressCeiling = Mathf.Clamp01(progressCeiling);
_progressCeilingBeforeFinalize = progressCeiling;

if (loadingScreen != null)
{
loadingScreen.Show();
loadingScreen.SetProgress(0f);
}

float realStreamStartProgress = Mathf.Clamp01(loadProgressStart);
if (loadingScreen != null && useFakeProgressForTransition)
{
float fakePreloadMin = Mathf.Min(fakePreloadEndMin, fakePreloadEndMax);
float fakePreloadMax = Mathf.Max(fakePreloadEndMin, fakePreloadEndMax);
float fakePreloadTarget = UnityEngine.Random.Range(fakePreloadMin, fakePreloadMax);
fakePreloadTarget = Mathf.Clamp(fakePreloadTarget, 0f, loadProgressStreamEnd);

float fakePreDurationMin = Mathf.Min(fakePreloadDurationMin, fakePreloadDurationMax);
float fakePreDurationMax = Mathf.Max(fakePreloadDurationMin, fakePreloadDurationMax);
float fakePreDuration = UnityEngine.Random.Range(fakePreDurationMin, fakePreDurationMax);

yield return SimulateProgress(0f, fakePreloadTarget, fakePreDuration);
realStreamStartProgress = Mathf.Max(realStreamStartProgress, fakePreloadTarget);
}
else if (loadingScreen != null)
{
loadingScreen.SetProgress(realStreamStartProgress);
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
loadingScreen.SetProgress(Mathf.Lerp(realStreamStartProgress, loadProgressStreamEnd, streamProgress));
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
loadingScreen.SetProgress(Mathf.Lerp(loadProgressStreamEnd, Mathf.Min(loadProgressMinTimeEnd, progressCeiling), normalizedMin));

yield return null;
}

if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Min(loadProgressMinTimeEnd, progressCeiling));

TrySetLocalizationLoadGateDeferring(true);

asyncOp.allowSceneActivation = true;
while (!asyncOp.isDone)
{
timer += Time.unscaledDeltaTime;

if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Min(loadProgressActivationEnd, progressCeiling));

yield return null;
}

yield return ExecuteSceneLoadTasks(sceneName, data);
TrySetLocalizationLoadGateDeferring(false);
yield return DrainDeferredLocalizationUpdates();

bool skipSceneReadyWait = data != null && data.SkipSceneReadyWait;
bool shouldWaitForSceneReady = waitForSceneReadySignal && !skipSceneReadyWait && SceneHasReadySignalReceiver();

if (shouldWaitForSceneReady)
{
float sceneReadyWaited = 0f;
float sceneReadyTimeout = Mathf.Max(0f, sceneReadyTimeoutSeconds);

while (!_sceneReady && sceneReadyWaited < sceneReadyTimeout)
{
sceneReadyWaited += Time.unscaledDeltaTime;
timer += Time.unscaledDeltaTime;

if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Min(loadProgressSceneReadyEnd, progressCeiling));

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
loadingScreen.SetProgress(Mathf.Min(loadProgressMusicEnd, progressCeiling));

yield return WaitForMusicReady(sceneName, data);

if (logLoadTimings)
{
float total = Time.realtimeSinceStartup - startedAt;
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Hide loading for '{sceneName}'. total={total:0.###}s");
}

if (loadingScreen != null)
{
if (useFakeProgressForTransition)
{
float fakeFinalizeDurationMinValue = Mathf.Min(fakeFinalizeDurationMin, fakeFinalizeDurationMax);
float fakeFinalizeDurationMaxValue = Mathf.Max(fakeFinalizeDurationMin, fakeFinalizeDurationMax);
float fakeFinalizeDuration = UnityEngine.Random.Range(fakeFinalizeDurationMinValue, fakeFinalizeDurationMaxValue);
yield return SimulateProgress(progressCeiling, 1f, fakeFinalizeDuration);
}
else
{
loadingScreen.SetProgress(1f);
}

loadingScreen.Hide();
}

_progressCeilingBeforeFinalize = 1f;
}

private IEnumerator ExecuteSceneLoadTasks(string sceneName, SceneTransitionData data)
{
if (!runSceneLoadTasks || (data != null && data.SkipSceneLoadTasks))
yield break;

var tasks = CollectSceneLoadTasks();
if (tasks.Count == 0)
yield break;

float totalWeight = 0f;
for (int i = 0; i < tasks.Count; i++)
totalWeight += tasks[i].Weight;

if (totalWeight <= 0f)
yield break;

float completedWeight = 0f;
for (int i = 0; i < tasks.Count; i++)
{
var task = tasks[i];
float taskVisualProgress = 0f;
IEnumerator routine = null;

try
{
routine = task.Run();
}
catch (Exception ex)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Load task '{task.Name}' init failed for '{sceneName}': {ex.Message}");
}

if (routine != null)
{
float taskStartedAt = Time.realtimeSinceStartup;
float taskTimeout = Mathf.Max(0f, maxSingleLoadTaskSeconds);

while (true)
{
if (taskTimeout > 0f)
{
float taskElapsed = Time.realtimeSinceStartup - taskStartedAt;
if (taskElapsed >= taskTimeout)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Load task '{task.Name}' timed out for '{sceneName}' after {taskElapsed:0.###}s. Continuing load.");
break;
}
}

bool moved;
object yielded;
try
{
moved = routine.MoveNext();
yielded = moved ? routine.Current : null;
}
catch (Exception ex)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Load task '{task.Name}' execution failed for '{sceneName}': {ex.Message}");
break;
}

if (!moved)
break;

taskVisualProgress = Mathf.MoveTowards(taskVisualProgress, 1f, Mathf.Max(0.01f, runningTaskProgressSpeed) * Time.unscaledDeltaTime);
UpdateTaskQueueProgress(completedWeight + task.Weight * taskVisualProgress, totalWeight);
yield return yielded;
}
}

completedWeight += task.Weight;
UpdateTaskQueueProgress(completedWeight, totalWeight);
yield return null;
}
}

private void TryResolveLoadingScreenFromActiveScene()
{
if (loadingScreen != null)
return;

var scene = SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded)
return;

var roots = scene.GetRootGameObjects();
for (int i = 0; i < roots.Length; i++)
{
var root = roots[i];
if (root == null)
continue;

var candidates = root.GetComponentsInChildren<MonoBehaviour>(true);
for (int j = 0; j < candidates.Length; j++)
{
if (candidates[j] is ILoadingScreen screen)
{
loadingScreen = screen;
return;
}
}
}
}

private void UpdateTaskQueueProgress(float completedWeight, float totalWeight)
{
if (loadingScreen == null || totalWeight <= 0f)
return;

float normalized = Mathf.Clamp01(completedWeight / totalWeight);
float tasksTarget = Mathf.Lerp(loadProgressActivationEnd, loadProgressTasksEnd, normalized);
loadingScreen.SetProgress(Mathf.Min(tasksTarget, _progressCeilingBeforeFinalize));
}

private IEnumerator SimulateProgress(float from, float to, float durationSeconds)
{
if (loadingScreen == null)
yield break;

float start = Mathf.Clamp01(from);
float end = Mathf.Clamp01(to);

if (durationSeconds <= 0f || Mathf.Approximately(start, end))
{
loadingScreen.SetProgress(end);
yield break;
}

float elapsed = 0f;
while (elapsed < durationSeconds)
{
elapsed += Time.unscaledDeltaTime;
float t = Mathf.Clamp01(elapsed / durationSeconds);
loadingScreen.SetProgress(Mathf.Lerp(start, end, t));
yield return null;
}

loadingScreen.SetProgress(end);
}

private List<SceneLoadTask> CollectSceneLoadTasks()
{
var tasks = new List<SceneLoadTask>();

for (int s = 0; s < SceneManager.sceneCount; s++)
{
var scene = SceneManager.GetSceneAt(s);
if (!scene.IsValid() || !scene.isLoaded)
continue;

var roots = scene.GetRootGameObjects();
for (int i = 0; i < roots.Length; i++)
{
var root = roots[i];
if (root == null)
continue;

var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
for (int j = 0; j < behaviours.Length; j++)
{
if (behaviours[j] is ISceneLoadTaskProvider provider)
{
try
{
provider.CollectLoadTasks(tasks);
}
catch (Exception ex)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] CollectLoadTasks failed in '{behaviours[j].name}': {ex.Message}");
}

continue;
}

TryCollectLoadTaskViaReflection(behaviours[j], tasks);
}
}
}

return tasks;
}

private void TryCollectLoadTaskViaReflection(MonoBehaviour behaviour, List<SceneLoadTask> tasks)
{
if (behaviour == null || tasks == null)
return;

var method = behaviour.GetType().GetMethod(
"TryCreateSceneLoadTask",
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
null,
new[] { typeof(string).MakeByRefType(), typeof(float).MakeByRefType(), typeof(IEnumerator).MakeByRefType() },
null);

if (method == null || method.ReturnType != typeof(bool))
return;

var args = new object[] { null, 0f, null };
try
{
var okObj = method.Invoke(behaviour, args);
if (okObj is not bool ok || !ok)
return;

if (args[2] is not IEnumerator routine || routine == null)
return;

var name = args[0] as string;
float weight = args[1] is float w ? w : 0.25f;
tasks.Add(new SceneLoadTask(name, weight, () => routine));
}
catch (Exception ex)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Reflection load-task failed in '{behaviour.name}': {ex.Message}");
}
}

private IEnumerator WaitForMusicReady(string sceneName, SceneTransitionData data)
{
if (data != null && data.SkipMusicWait)
yield break;

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

private static void TrySetLocalizationLoadGateDeferring(bool isDeferring)
{
var type = Type.GetType("LocalizationLoadGate, UDA2.Localization");
if (type == null)
return;

var methodName = isDeferring ? "BeginDeferring" : "EndDeferring";
var method = type.GetMethod(
methodName,
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
null,
Type.EmptyTypes,
null);

if (method == null)
return;

try
{
method.Invoke(null, null);
}
catch (Exception)
{
}
}

private IEnumerator DrainDeferredLocalizationUpdates()
{
if (!drainDeferredLocalizationAfterActivation)
yield break;

var type = Type.GetType("LocalizationLoadGate, UDA2.Localization");
if (type == null)
yield break;

var hasPendingProperty = type.GetProperty(
"HasPending",
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
null,
typeof(bool),
Type.EmptyTypes,
null);

var drainBatchMethod = type.GetMethod(
"DrainBatch",
System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
null,
new[] { typeof(int) },
null);

if (hasPendingProperty == null || drainBatchMethod == null)
yield break;

int batchSize = Mathf.Max(1, maxDeferredLocalizationPerFrame);
float timeout = Mathf.Max(0f, deferredLocalizationDrainTimeoutSeconds);
float startedAt = Time.realtimeSinceStartup;

while (true)
{
bool hasPending;
try
{
var value = hasPendingProperty.GetValue(null, null);
hasPending = value is bool b && b;
}
catch
{
yield break;
}

if (!hasPending)
yield break;

if (timeout > 0f && Time.realtimeSinceStartup - startedAt >= timeout)
yield break;

int drained;
try
{
var value = drainBatchMethod.Invoke(null, new object[] { batchSize });
drained = value is int count ? count : 0;
}
catch
{
yield break;
}

if (drained <= 0)
yield break;

yield return null;
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
