using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

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

[Header("Deferred Localization")]
[SerializeField] private bool drainDeferredLocalizationAfterActivation = true;
[SerializeField, Min(1)] private int maxDeferredLocalizationPerFrame = 32;
[SerializeField, Min(0f)] private float deferredLocalizationDrainTimeoutSeconds = 0.12f;

private bool _sceneReady;
private ILoadingScreen loadingScreen;
private Coroutine _loadCoroutine;
private float _progressCeilingBeforeFinalize = 1f;
private AsyncOperation _pendingUnloadOp;
private bool _loadingScreenVisible;
private float _loadingScreenBootstrapProgress = -1f;

// Background preloaded scenes: scene name -> suspended AsyncOperation (allowSceneActivation=false).
// Scenes loaded here sit in memory at 90%, ready to activate instantly.
private readonly Dictionary<string, AsyncOperation> _preloadedScenes = new();
private readonly Dictionary<string, Coroutine> _preloadRoutines = new();

// Instant fullscreen black cover shown the moment a transition begins.
// It hides the source scene immediately while LoadingScene is still loading,
// eliminating the 2-3 second window where the player sees the old scene.
// Auto-created in Awake if not assigned via Inspector.
[Header("Transition Cover")]
[Tooltip("Optional: assign a CanvasGroup on a fullscreen black panel. Auto-created at runtime if left empty.")]
[SerializeField] private CanvasGroup transitionCoverGroup;

// --- Cached reflection delegates (resolved once in Awake) ---
// Avoids repeated Type.GetType / GetMethod / GetField / GetProperty calls
// on every scene transition and every frame during music-wait loops.
private Action _locBeginDeferring;
private Action _locEndDeferring;
private Func<bool> _locHasPending;
private Func<int, int> _locDrainBatch;
private Func<object> _audioGetInstance;
private Func<object, bool> _audioIsMusicPlaying;
private Func<object, string, bool> _audioWillPlayMusicForScene;
private Action<object, string> _audioPreloadSceneMusic;

private void BuildReflectionCache()
{
    // Localization gate
    var locType = Type.GetType("LocalizationLoadGate, UDA2.Localization");
    if (locType != null)
    {
        var mBegin = locType.GetMethod("BeginDeferring", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        if (mBegin != null) _locBeginDeferring = (Action)Delegate.CreateDelegate(typeof(Action), mBegin, false);

        var mEnd = locType.GetMethod("EndDeferring", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        if (mEnd != null) _locEndDeferring = (Action)Delegate.CreateDelegate(typeof(Action), mEnd, false);

        var pHas = locType.GetProperty("HasPending", BindingFlags.Public | BindingFlags.Static, null, typeof(bool), Type.EmptyTypes, null);
        if (pHas != null) { var getter = pHas.GetGetMethod(); _locHasPending = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), getter, false); }

        var mDrain = locType.GetMethod("DrainBatch", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
        if (mDrain != null) _locDrainBatch = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), mDrain, false);
    }

    // AudioManager
    var audioType = Type.GetType("UDA2.Audio.AudioManager, UDA2.Audio");
    if (audioType != null)
    {
        var fInstance = audioType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        if (fInstance != null) _audioGetInstance = () => fInstance.GetValue(null);

        var pPlaying = audioType.GetProperty("IsMusicPlaying", BindingFlags.Public | BindingFlags.Instance);
        if (pPlaying != null) { var g = pPlaying.GetGetMethod(); _audioIsMusicPlaying = obj => (bool)(g.Invoke(obj, null) ?? false); }

        var mWill = audioType.GetMethod("WillPlayMusicForScene", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
        if (mWill != null) _audioWillPlayMusicForScene = (obj, s) => (bool)(mWill.Invoke(obj, new object[] { s }) ?? false);

        var mPreload = audioType.GetMethod("PreloadSceneMusicAudioData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
        if (mPreload != null) _audioPreloadSceneMusic = (obj, s) => mPreload.Invoke(obj, new object[] { s });
    }
}

// The single EventSystem and AudioListener that live in DDOL.
// Duplicates from loaded scenes are removed in OnAnySceneLoaded.
private EventSystem _ddolEventSystem;
private AudioListener _ddolAudioListener;

private void Awake()
{
if (Instance != null)
{
Destroy(gameObject);
return;
}
Instance = this;
DontDestroyOnLoad(gameObject);
EnsureTransitionCover();
EnsureEventSystem();
EnsureAudioListener();
BuildReflectionCache();
SceneManager.sceneLoaded += OnAnySceneLoaded;
// Load LoadingScene once and keep it alive in DDOL for the entire session.
// It is never unloaded — we simply Show/Hide it each transition.
StartCoroutine(EnsureLoadingSceneLoaded());
}
private bool _loadingSceneReady;

/// Loads LoadingScene additively once at startup and keeps it loaded forever.
/// It is never unloaded — we simply Show/Hide the loader UI each transition.
/// DontDestroyOnLoad is NOT used: keeping it as a named scene lets us
/// SetActiveScene on it during transitions.
private IEnumerator EnsureLoadingSceneLoaded()
{
    // Already loaded (e.g. editor play-from-loading-scene).
    var existing = SceneManager.GetSceneByName(LoadingSceneName);
    if (existing.IsValid() && existing.isLoaded)
    {
        TryResolveLoadingScreen();
        if (!IsTransitionInProgress && loadingScreen != null)
            loadingScreen.Hide();
        _loadingSceneReady = true;
        yield break;
    }

    // Prevent "There can be only one active Event System" during startup
    // additive load of LoadingScene. It will be re-enabled in OnAnySceneLoaded.
    SetDdolSingletonsActive(false);
    var op = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
    if (op == null)
    {
        SetDdolSingletonsActive(true);
        if (logLoadTimings)
            UDA2.Logging.Logger.LogInfo("[SceneFlow] Could not load LoadingScene at startup.");
        yield break;
    }

    while (!op.isDone)
        yield return null;

    TryResolveLoadingScreen();
    if (!IsTransitionInProgress && loadingScreen != null)
        loadingScreen.Hide();
    _loadingSceneReady = true;
    if (logLoadTimings)
        UDA2.Logging.Logger.LogInfo("[SceneFlow] LoadingScene loaded (persistent, never unloaded).");
}

private void TryResolveLoadingScreen()
{
    if (loadingScreen != null) return;
    TryResolveLoadingScreenFromActiveScene();
    if (loadingScreen == null)
    {
        var scene = SceneManager.GetSceneByName(LoadingSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var candidates = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] is ILoadingScreen screen)
                    {
                        loadingScreen = screen;
                        break;
                    }
                }

                if (loadingScreen != null)
                    break;
            }
        }
    }

    if (loadingScreen == null && logLoadTimings)
        UDA2.Logging.Logger.LogInfo("[SceneFlow] ILoadingScreen not found after resolve attempts.");
}

private void EnsureTransitionCover()
{
if (transitionCoverGroup != null)
return;

var coverGo = new GameObject("[TransitionCover]");
coverGo.transform.SetParent(transform, false);

var canvas = coverGo.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 9999; // Always on top.

coverGo.AddComponent<UnityEngine.UI.CanvasScaler>();

var panelGo = new GameObject("Panel");
panelGo.transform.SetParent(coverGo.transform, false);
var rt = panelGo.AddComponent<RectTransform>();
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
var img = panelGo.AddComponent<UnityEngine.UI.Image>();
img.color = Color.black;
img.raycastTarget = true;

transitionCoverGroup = coverGo.AddComponent<CanvasGroup>();
transitionCoverGroup.alpha = 0f;
transitionCoverGroup.blocksRaycasts = false;
transitionCoverGroup.interactable = false;
}

private void ShowTransitionCover()
{
if (transitionCoverGroup == null) return;
transitionCoverGroup.alpha = 1f;
transitionCoverGroup.blocksRaycasts = true;
}

private void HideTransitionCover()
{
if (transitionCoverGroup == null) return;
transitionCoverGroup.alpha = 0f;
transitionCoverGroup.blocksRaycasts = false;
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
{
Instance = null;
SceneManager.sceneLoaded -= OnAnySceneLoaded;
}

SetTransitionInProgress(false);
}

/// Finds the existing EventSystem in the bootstrap scene and moves it to DDOL.
/// If none exists, the first one encountered in OnAnySceneLoaded will be promoted.
private void EnsureEventSystem()
{
_ddolEventSystem = FindFirstObjectByType<EventSystem>();
if (_ddolEventSystem != null)
    DontDestroyOnLoad(_ddolEventSystem.gameObject);
}

/// Creates a dedicated AudioListener on this DDOL object.
/// All AudioListeners found in loaded scenes are destroyed (component only,
/// not the GameObject) in OnAnySceneLoaded, so only this one remains active.
private void EnsureAudioListener()
{
_ddolAudioListener = GetComponent<AudioListener>();
if (_ddolAudioListener == null)
    _ddolAudioListener = gameObject.AddComponent<AudioListener>();

// Remove any AudioListeners already present in the current scene.
var existing = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
foreach (var al in existing)
    if (al != _ddolAudioListener)
        Destroy(al);
}

/// Called by Unity after every scene load.
/// Removes duplicate EventSystems and AudioListeners from the newly loaded scene
/// so Unity never sees more than one of each active simultaneously.
private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
{
var roots = scene.GetRootGameObjects();

// --- EventSystem deduplication ---
foreach (var root in roots)
{
    foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
    {
        if (_ddolEventSystem == null || !_ddolEventSystem)
        {
            // No DDOL EventSystem yet — promote this one.
            _ddolEventSystem = es;
            DontDestroyOnLoad(es.gameObject);
        }
        else
        {
            // Disable the component only — never destroy the GameObject.
            // Destroying es.gameObject risks wiping other components on the
            // same object (e.g. LoadingScreenController on a shared root).
            es.enabled = false;
        }
    }
}

// --- AudioListener deduplication ---
// Disable the component only (never destroy it) so we don't risk removing
// things like Camera or CanvasRenderer from the same GameObject.
foreach (var root in roots)
    foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
        al.enabled = false;

// Re-enable the DDOL singletons now that all duplicates are cleaned up.
SetDdolSingletonsActive(true);
}

/// Temporarily disables or re-enables the DDOL EventSystem only.
/// Must be called BEFORE every additive scene activation so that Unity does not
/// see two active EventSystems at once (which fires an OnEnable warning).
/// AudioListener is intentionally excluded: disabling it creates a "no audio
/// listeners" gap; the brief "2 listeners" warning from a duplicate is harmless
/// and disappears as soon as OnAnySceneLoaded destroys the scene copy.
private void SetDdolSingletonsActive(bool active)
{
    if (_ddolEventSystem != null)
        _ddolEventSystem.enabled = active;
}

public void RegisterLoadingScreen(ILoadingScreen screen)
{
loadingScreen = screen;

// LoadingScene is persistent; keep it hidden outside real transitions
// so startup scenes like Splash/MainMenu are not occluded.
if (!IsTransitionInProgress && loadingScreen != null)
{
    loadingScreen.Hide();
    _loadingScreenVisible = false;
}
}

public void UnregisterLoadingScreen(ILoadingScreen screen)
{
if (loadingScreen == screen)
{
_loadingScreenVisible = false;
loadingScreen = null;
}
}

public void NotifySceneReady()
{
_sceneReady = true;
}

// --- Background Preloading API ---

/// <summary>
/// Starts loading <paramref name="sceneName"/> in the background using Additive mode
/// with allowSceneActivation=false. The scene sits at 90% in memory, ready to
/// activate instantly when LoadScene() is called for the same name.
/// Safe to call multiple times for the same scene — only one load runs at a time.
/// Typical usage: call from the scene the player is likely to leave next
/// (e.g. MonsterCaveScene calls PreloadScene("FightScene") on Awake).
/// </summary>
public void PreloadScene(string sceneName)
{
if (string.IsNullOrWhiteSpace(sceneName)) return;
if (_preloadedScenes.ContainsKey(sceneName)) return;  // already loaded or loading
if (_preloadRoutines.ContainsKey(sceneName)) return;

var routine = StartCoroutine(BackgroundPreloadRoutine(sceneName));
_preloadRoutines[sceneName] = routine;
}

/// <summary>
/// Cancels and discards a background preload if it is still in progress.
/// Scenes already at 90% cannot be truly cancelled by Unity — they will be
/// unloaded via UnloadSceneAsync instead.
/// </summary>
public void CancelPreload(string sceneName)
{
if (string.IsNullOrWhiteSpace(sceneName)) return;

if (_preloadRoutines.TryGetValue(sceneName, out var routine))
{
if (routine != null) StopCoroutine(routine);
_preloadRoutines.Remove(sceneName);
}

if (_preloadedScenes.TryGetValue(sceneName, out var op))
{
_preloadedScenes.Remove(sceneName);
// Scene is Additive and suspended — we must activate then immediately unload.
SetDdolSingletonsActive(false);
op.allowSceneActivation = true;
StartCoroutine(UnloadAfterActivation(sceneName));
}
}

private IEnumerator UnloadAfterActivation(string sceneName)
{
// Wait one frame for activation to complete, then unload.
yield return null;
yield return null;
var scene = SceneManager.GetSceneByName(sceneName);
if (scene.IsValid() && scene.isLoaded)
SceneManager.UnloadSceneAsync(scene);
}

private IEnumerator BackgroundPreloadRoutine(string sceneName)
{
// Load additively with activation suspended — Unity loads assets in background
// threads up to 90% without touching the main scene.
var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
if (op == null)
{
_preloadRoutines.Remove(sceneName);
yield break;
}

op.allowSceneActivation = false;

if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Background preload started: '{sceneName}'");

// Yield until Unity has loaded all assets (progress reaches 0.9 = 90%).
while (op.progress < 0.9f)
yield return null;

_preloadedScenes[sceneName] = op;
_preloadRoutines.Remove(sceneName);

if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Background preload ready: '{sceneName}'");
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
ShowTransitionCover();
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
yield break;
}

// Signal transition start immediately: hide global UI and show instant black cover
// in the same frame as the click, before any scene loading begins.
SetTransitionInProgress(true);
ShowTransitionCover();

bool useFakeProgressForTransition = useFakeProgressEnvelope && !(data != null && data.DisableFakeProgressEnvelope);

// LoadingScene is always alive in DDOL — just wait for it if startup load is
// still in progress (very unlikely after the first few frames).
while (!_loadingSceneReady)
    yield return null;

_sceneReady = false;
string sourceSceneName = SceneManager.GetActiveScene().name;

// Resolve loadingScreen from the persistent DDOL LoadingScene.
if (loadingScreen == null)
    TryResolveLoadingScreen();

// Show loader immediately BEFORE source-scene unload/target-scene load,
// so the player sees loading UI instead of a frozen black cover.
if (loadingScreen != null)
{
    loadingScreen.Show();
    _loadingScreenVisible = true;
    loadingScreen.SetProgress(0f);

    if (useFakeProgressForTransition)
    {
        float preMin = Mathf.Min(fakePreloadEndMin, fakePreloadEndMax);
        float preMax = Mathf.Max(fakePreloadEndMin, fakePreloadEndMax);
        float bootstrapTarget = Mathf.Clamp01(UnityEngine.Random.Range(preMin, preMax));
        yield return SimulateProgress(0f, bootstrapTarget, 0.15f);
        _loadingScreenBootstrapProgress = bootstrapTarget;
    }
    else
    {
        _loadingScreenBootstrapProgress = Mathf.Clamp01(loadProgressStart);
        loadingScreen.SetProgress(_loadingScreenBootstrapProgress);
    }

    // Loader is visible now, so remove the hard black cover.
    HideTransitionCover();
}

SetDdolSingletonsActive(false);

// Make LoadingScene the active scene so audio, lighting and new objects
// belong to it while the transition runs.
var loadingScene = SceneManager.GetSceneByName(LoadingSceneName);
if (loadingScene.IsValid())
    SceneManager.SetActiveScene(loadingScene);

// 3) Asynchronously unload the source scene.
//    UnloadSceneAsync is truly async: OnDestroy calls are spread across frames,
//    so the loading screen continues to render and animate without freezing.
//    We do NOT yield on it — unload runs in the background while the loader is visible.
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Unloading source scene '{sourceSceneName}' async.");
var unloadOp = SceneManager.UnloadSceneAsync(sourceSceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
if (unloadOp == null && logLoadTimings)
    UDA2.Logging.Logger.LogInfo($"[SceneFlow] UnloadSceneAsync returned null for '{sourceSceneName}' — scene may not be loaded.");
// Fire-and-forget: target scene load starts immediately, unload runs in parallel.

// 4) Load the target scene with staged progress and waits.
// Pass the unload op so LoadSceneRoutine can wait for MonsterCave/source scene to
// finish unloading before activating the target scene (avoids Single-mode freeze).
_pendingUnloadOp = unloadOp;
yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
// LoadingScene stays loaded in DDOL — no unload needed.
}
finally
{
HideTransitionCover();
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
// If a background preload exists for sceneName, its suspended AsyncOperation is reused
// instead of starting a fresh load — the scene is already at 90% in memory.
private IEnumerator LoadSceneRoutine(string sceneName, SceneTransitionData data, float minLoadingTime)
{
float startedAt = Time.realtimeSinceStartup;
_sceneReady = false;
bool useFakeProgressForTransition = useFakeProgressEnvelope && !(data != null && data.DisableFakeProgressEnvelope);

// Consume preloaded op if available — skip the async load entirely.
bool hadPreload = _preloadedScenes.TryGetValue(sceneName, out var preloadedOp);
if (hadPreload)
{
_preloadedScenes.Remove(sceneName);
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Using preloaded scene: '{sceneName}'");
}

float progressCeiling = loadProgressMusicEnd;
if (loadingScreen != null && useFakeProgressForTransition)
{
float fakeStartMin = Mathf.Min(fakeFinalizeStartMin, fakeFinalizeStartMax);
float fakeStartMax = Mathf.Max(fakeFinalizeStartMin, fakeFinalizeStartMax);
progressCeiling = UnityEngine.Random.Range(fakeStartMin, fakeStartMax);
}

progressCeiling = Mathf.Clamp01(progressCeiling);
_progressCeilingBeforeFinalize = progressCeiling;

float realStreamStartProgress = Mathf.Clamp01(loadProgressStart);
if (_loadingScreenBootstrapProgress >= 0f)
{
realStreamStartProgress = Mathf.Max(realStreamStartProgress, _loadingScreenBootstrapProgress);
_loadingScreenBootstrapProgress = -1f;
}

if (loadingScreen != null)
{
// Loader is ready to take over - hide the instant black cover so the
// loader UI (with background art, progress bar, etc.) is visible.
HideTransitionCover();
if (!_loadingScreenVisible)
{
loadingScreen.Show();
_loadingScreenVisible = true;
}
loadingScreen.SetProgress(realStreamStartProgress);
}

if (loadingScreen != null && useFakeProgressForTransition)
{
float fakePreloadMin = Mathf.Min(fakePreloadEndMin, fakePreloadEndMax);
float fakePreloadMax = Mathf.Max(fakePreloadEndMin, fakePreloadEndMax);
float fakePreloadTarget = UnityEngine.Random.Range(fakePreloadMin, fakePreloadMax);
fakePreloadTarget = Mathf.Clamp(fakePreloadTarget, realStreamStartProgress, loadProgressStreamEnd);

float fakePreDurationMin = Mathf.Min(fakePreloadDurationMin, fakePreloadDurationMax);
float fakePreDurationMax = Mathf.Max(fakePreloadDurationMin, fakePreloadDurationMax);
float fakePreDuration = UnityEngine.Random.Range(fakePreDurationMin, fakePreDurationMax);

yield return SimulateProgress(realStreamStartProgress, fakePreloadTarget, fakePreDuration);
realStreamStartProgress = Mathf.Max(realStreamStartProgress, fakePreloadTarget);
}
else if (loadingScreen != null)
{
loadingScreen.SetProgress(realStreamStartProgress);
}

float timer = 0f;
TryPreloadSceneMusic(sceneName);

// Use preloaded op if available (scene already at 90% in memory),
// otherwise start a fresh async load.
AsyncOperation asyncOp;
if (hadPreload && preloadedOp != null)
{
asyncOp = preloadedOp;
// Scene is already fully loaded in the background — jump straight to minTime wait.
timer = minLoadingTime; // treat load as instantly done
if (loadingScreen != null)
loadingScreen.SetProgress(loadProgressStreamEnd);
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Preloaded scene '{sceneName}' activated instantly.");
}
else
{
asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
if (asyncOp == null)
{
if (logLoadTimings)
UDA2.Logging.Logger.LogInfo($"[SceneFlow] Failed to start async load for '{sceneName}'.");
if (loadingScreen != null)
{
loadingScreen.Hide();
_loadingScreenVisible = false;
}
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
}

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

// Wait for the source scene to finish unloading before activating the target scene.
// Without this, Single-mode activation would encounter remaining source scene objects
// and destroy them synchronously, causing the exact freeze we're trying to avoid.
if (_pendingUnloadOp != null)
{
while (!_pendingUnloadOp.isDone)
{
if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Min(loadProgressActivationEnd * 0.5f, progressCeiling));
yield return null;
}
_pendingUnloadOp = null;
}

SetDdolSingletonsActive(false);
asyncOp.allowSceneActivation = true;
while (!asyncOp.isDone)
{
timer += Time.unscaledDeltaTime;

if (loadingScreen != null)
loadingScreen.SetProgress(Mathf.Min(loadProgressActivationEnd, progressCeiling));

yield return null;
}

// Make the newly loaded scene the active scene so that:
// a) the next transition captures the correct sourceSceneName via GetActiveScene();
// b) lighting, skybox and audio sources belong to the right scene.
var activatedScene = SceneManager.GetSceneByName(sceneName);
if (activatedScene.IsValid() && activatedScene.isLoaded)
    SceneManager.SetActiveScene(activatedScene);

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
_loadingScreenVisible = false;
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
if (!preloadSceneMusicBeforeActivation || string.IsNullOrWhiteSpace(sceneName)) return;
if (_audioGetInstance == null || _audioPreloadSceneMusic == null) return;
try { var mgr = _audioGetInstance(); if (mgr != null) _audioPreloadSceneMusic(mgr, sceneName); }
catch { }
}

private static void TrySetLocalizationLoadGateDeferring(bool isDeferring)
{
if (Instance == null) return;
try { (isDeferring ? Instance._locBeginDeferring : Instance._locEndDeferring)?.Invoke(); }
catch (Exception) { }
}

private IEnumerator DrainDeferredLocalizationUpdates()
{
if (!drainDeferredLocalizationAfterActivation || _locHasPending == null || _locDrainBatch == null)
yield break;

int batchSize = Mathf.Max(1, maxDeferredLocalizationPerFrame);
float timeout = Mathf.Max(0f, deferredLocalizationDrainTimeoutSeconds);
float startedAt = Time.realtimeSinceStartup;

while (true)
{
bool hasPending;
try { hasPending = _locHasPending(); }
catch { yield break; }

if (!hasPending) yield break;
if (timeout > 0f && Time.realtimeSinceStartup - startedAt >= timeout) yield break;

int drained;
try { drained = _locDrainBatch(batchSize); }
catch { yield break; }

if (drained <= 0) yield break;
yield return null;
}
}

private bool TryGetAudioManagerSingleton(out object audioManager)
{
audioManager = null;
if (_audioGetInstance == null) return false;
try { audioManager = _audioGetInstance(); }
catch { return false; }
return audioManager != null;
}

private bool IsMusicPlaying(object audioManager)
{
if (audioManager == null || _audioIsMusicPlaying == null) return false;
try { return _audioIsMusicPlaying(audioManager); }
catch { return false; }
}

private bool WillPlayMusicForScene(object audioManager, string sceneName)
{
if (audioManager == null || _audioWillPlayMusicForScene == null) return false;
try { return _audioWillPlayMusicForScene(audioManager, sceneName); }
catch { return false; }
}

private static bool SceneHasReadySignalReceiver()
{
var scene = SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded) return false;

var roots = scene.GetRootGameObjects();
for (int i = 0; i < roots.Length; i++)
{
var behaviours = roots[i]?.GetComponentsInChildren<MonoBehaviour>(true);
if (behaviours == null) continue;
for (int j = 0; j < behaviours.Length; j++)
{
if (behaviours[j] is ISceneReady) return true;
}
}
return false;
}
}
}
