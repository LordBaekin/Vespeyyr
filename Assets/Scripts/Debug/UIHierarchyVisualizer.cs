using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Added TextMeshPro namespace
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI Hierarchy Visualizer for debugging UI issues across scenes
/// </summary>
public class UIHierarchyVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TextMeshProUGUI outputText; // Changed to TextMeshProUGUI
    [SerializeField] private bool autoUpdateEveryFrame = false;
    [SerializeField] private float autoUpdateInterval = 0.5f; // Update every half second instead of every frame
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private KeyCode refreshKey = KeyCode.F10;
    [SerializeField] private KeyCode copyKey = KeyCode.F11;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [SerializeField] private bool showInactiveObjects = true;
    [SerializeField] private bool showComponentDetails = true;
    [SerializeField] private bool logToConsole = false;
    [SerializeField] private bool trackSceneChanges = true;

    [Header("Pagination")]
    [SerializeField] private int maxPageSize = 10000; // Characters per page
    [SerializeField] private KeyCode nextPageKey = KeyCode.F8; // Key to navigate to next page
    [SerializeField] private KeyCode prevPageKey = KeyCode.F7; // Key to navigate to previous page
    private int currentPage = 0;
    private int totalPages = 1;

    [Header("Display Options")]
    [SerializeField] private float panelWidth = 0.3f; // 30% of screen width
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 14;
    [SerializeField]
    private string[] ignoredComponents = new string[] {
        "RectTransform", "CanvasRenderer", "CanvasGroup"
    };

    // Private variables
    private bool isVisible = false;
    private Canvas debugCanvas;
    private TextMeshProUGUI titleText; // Changed to TextMeshProUGUI
    private TextMeshProUGUI sceneInfoText; // Changed to TextMeshProUGUI
    private GameObject infoPanel;
    private float lastUpdateTime = 0f;
    private string currentScene = "";
    private StringBuilder logBuilder = new StringBuilder();
    private string currentHierarchyText = "";
    private TextMeshProUGUI notificationText; // Changed to TextMeshProUGUI
    private TextMeshProUGUI paginationInfoText; // Added for pagination info
    private GameObject notificationPanel;
    private GameObject paginationPanel; // Added for pagination panel
    private Coroutine hideNotificationCoroutine;

    // Singleton pattern
    public static UIHierarchyVisualizer Instance { get; private set; }

    void Awake()
    {
        // Singleton implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Create UI if not assigned
        if (outputText == null)
        {
            CreateDebugUI();
        }

        // Initially hide
        SetVisible(false);

        Debug.Log("[UIHierarchyVisualizer] Initialized. Press " + toggleKey + " to show/hide, " +
                  nextPageKey + "/" + prevPageKey + " to navigate pages, " +
                  copyKey + " to copy to clipboard, " + refreshKey + " to refresh.");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (trackSceneChanges)
        {
            currentScene = scene.name;
            StartCoroutine(RefreshAfterSceneLoad());
        }
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        // Wait for scene to fully initialize
        yield return new WaitForSeconds(0.5f);

        if (isVisible)
        {
            Debug.Log("[UIHierarchyVisualizer] Scene changed to: " + currentScene + ". Refreshing hierarchy...");
            RefreshHierarchy();

            if (sceneInfoText != null)
            {
                sceneInfoText.text = "Scene: " + currentScene;
            }
        }
    }

    void Update()
    {
        // Toggle visibility with key
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!isVisible);
        }

        // Close with escape key if visible
        if (isVisible && Input.GetKeyDown(closeKey))
        {
            SetVisible(false);
        }

        // Refresh with key
        if (isVisible && Input.GetKeyDown(refreshKey))
        {
            RefreshHierarchy();
            ShowNotification("Hierarchy refreshed", 1.5f);
        }

        // Copy to clipboard with key
        if (Input.GetKeyDown(copyKey))
        {
            CopyToClipboard();
        }

        // Page navigation - next page
        if (isVisible && Input.GetKeyDown(nextPageKey) && totalPages > 1)
        {
            currentPage = (currentPage + 1) % totalPages; // Cycle to first page after last
            UpdateCurrentPage();
            ShowNotification($"Page {currentPage + 1}/{totalPages}", 1f);
        }

        // Page navigation - previous page
        if (isVisible && Input.GetKeyDown(prevPageKey) && totalPages > 1)
        {
            currentPage = (currentPage - 1 + totalPages) % totalPages; // Cycle to last page from first
            UpdateCurrentPage();
            ShowNotification($"Page {currentPage + 1}/{totalPages}", 1f);
        }

        // Auto-update with interval if enabled
        if (isVisible && autoUpdateEveryFrame)
        {
            if (Time.time - lastUpdateTime >= autoUpdateInterval)
            {
                lastUpdateTime = Time.time;
                RefreshHierarchy();
            }
        }
    }

    /// <summary>
    /// Show or hide the UI hierarchy visualizer
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (debugCanvas != null)
        {
            debugCanvas.gameObject.SetActive(visible);
        }

        if (visible)
        {
            currentScene = SceneManager.GetActiveScene().name;
            if (sceneInfoText != null)
            {
                sceneInfoText.text = "Scene: " + currentScene;
            }

            RefreshHierarchy();
        }

        Debug.Log("[UIHierarchyVisualizer] " + (visible ? "Shown" : "Hidden"));
    }

    /// <summary>
    /// Copy the current hierarchy text to the system clipboard
    /// </summary>
    public void CopyToClipboard()
    {
        if (string.IsNullOrEmpty(currentHierarchyText))
        {
            // If not visible, refresh once to get current hierarchy
            if (!isVisible)
            {
                RefreshHierarchy();
            }
        }

        try
        {
            // Add a header with timestamp
            string textToCopy = $"UI HIERARCHY - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                          $"Scene: {SceneManager.GetActiveScene().name}\n\n" +
                          currentHierarchyText;

            // Set to clipboard
            GUIUtility.systemCopyBuffer = textToCopy;

            // Show notification and log
            Debug.Log("[UIHierarchyVisualizer] Hierarchy copied to clipboard");

            // Also output to console for easier access
            Debug.Log("=== UI HIERARCHY DUMP ===\n" + textToCopy);

            ShowNotification("Copied to clipboard!", 2f);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIHierarchyVisualizer] Failed to copy to clipboard: {ex.Message}");
            ShowNotification("Copy failed: " + ex.Message, 3f);
        }
    }

    /// <summary>
    /// Show a temporary notification message
    /// </summary>
    private void ShowNotification(string message, float duration)
    {
        if (notificationText == null || notificationPanel == null) return;

        // Cancel any existing hide coroutine
        if (hideNotificationCoroutine != null)
        {
            StopCoroutine(hideNotificationCoroutine);
        }

        // Show notification
        notificationPanel.SetActive(true);
        notificationText.text = message;

        // Start hide coroutine
        hideNotificationCoroutine = StartCoroutine(HideNotificationAfterDelay(duration));
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        hideNotificationCoroutine = null;
    }

    /// <summary>
    /// Manually trigger a refresh of the hierarchy display
    /// </summary>
    public void RefreshHierarchy()
    {
        if (outputText == null) return;

        logBuilder.Clear();
        logBuilder.AppendLine("=== UI HIERARCHY ===");
        logBuilder.AppendLine($"Time: {DateTime.Now.ToString("HH:mm:ss.fff")}");
        logBuilder.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
        logBuilder.AppendLine();

        // First, find all root objects (not under a Canvas)
        GameObject[] rootObjects = FindRootObjects();
        foreach (var rootObj in rootObjects)
        {
            logBuilder.AppendLine($"ROOT: {rootObj.name} (active: {rootObj.activeSelf})");
            VisualizeGameObject(rootObj, logBuilder, 1);
            logBuilder.AppendLine();
        }

        // Then find all canvases in the scene
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        // Sort canvases by sorting order
        Array.Sort(canvases, (a, b) => b.sortingOrder.CompareTo(a.sortingOrder));

        foreach (Canvas canvas in canvases)
        {
            // Skip our debug canvas to avoid infinite recursion
            if (canvas == debugCanvas) continue;

            // Check if this canvas is a child of another canvas we already processed
            bool isChildOfProcessedCanvas = false;
            foreach (var otherCanvas in canvases)
            {
                if (otherCanvas != canvas && canvas.transform.IsChildOf(otherCanvas.transform))
                {
                    isChildOfProcessedCanvas = true;
                    break;
                }
            }

            if (isChildOfProcessedCanvas) continue;

            string canvasPath = GetGameObjectPath(canvas.gameObject);
            logBuilder.AppendLine($"CANVAS: {canvas.name} (enabled: {canvas.gameObject.activeSelf}, sort: {canvas.sortingOrder})");
            logBuilder.AppendLine($"  Path: {canvasPath}");

            // For world space canvases, show their position
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Vector3 pos = canvas.transform.position;
                logBuilder.AppendLine($"  World Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            }

            VisualizeGameObject(canvas.gameObject, logBuilder, 1);
            logBuilder.AppendLine();
        }

        // Find any active camera
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cameras.Length > 0)
        {
            logBuilder.AppendLine("\n=== CAMERAS ===");
            foreach (var camera in cameras)
            {
                if (camera.gameObject.activeSelf && camera.enabled)
                {
                    logBuilder.AppendLine($"Active Camera: {camera.name} (depth: {camera.depth})");
                    logBuilder.AppendLine($"  Culling Mask: {camera.cullingMask}");
                    logBuilder.AppendLine($"  Clear Flags: {camera.clearFlags}");
                    logBuilder.AppendLine($"  Viewport Rect: {camera.rect}");
                }
            }
        }

        // Store the result
        currentHierarchyText = logBuilder.ToString();

        // Calculate pagination
        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)currentHierarchyText.Length / maxPageSize));

        // Reset to first page or stay on current page if it's still valid
        if (currentPage >= totalPages)
        {
            currentPage = 0;
        }

        // Update the displayed page
        UpdateCurrentPage();

        // Log to console if enabled
        if (logToConsole)
        {
            Debug.Log("=== UI HIERARCHY LOG ===\n" + currentHierarchyText);
        }

        lastUpdateTime = Time.time;
    }

    /// <summary>
    /// Update the displayed page based on the current page index
    /// </summary>
    private void UpdateCurrentPage()
    {
        if (string.IsNullOrEmpty(currentHierarchyText) || outputText == null)
            return;

        try
        {
            // Calculate page bounds
            int startIndex = currentPage * maxPageSize;
            int length = Mathf.Min(maxPageSize, currentHierarchyText.Length - startIndex);

            if (startIndex >= currentHierarchyText.Length)
            {
                // Reset to first page if out of bounds
                currentPage = 0;
                startIndex = 0;
                length = Mathf.Min(maxPageSize, currentHierarchyText.Length);
            }

            // Extract the current page content
            string pageContent = currentHierarchyText.Substring(startIndex, length);

            // Update the text
            outputText.text = pageContent;

            // Update pagination info
            if (paginationInfoText != null)
            {
                paginationInfoText.text = $"Page {currentPage + 1} of {totalPages} | {prevPageKey} Prev | {nextPageKey} Next";
            }

            // Update title to show pagination info
            if (titleText != null)
            {
                titleText.text = $"UI HIERARCHY VISUALIZER - Page {currentPage + 1}/{totalPages}";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIHierarchyVisualizer] Error updating page: {ex.Message}");

            if (outputText != null)
            {
                outputText.text = $"Error displaying page {currentPage + 1}: {ex.Message}\n\nTry using {copyKey} to copy to clipboard instead.";
            }
        }
    }

    private GameObject[] FindRootObjects()
    {
        List<GameObject> rootObjects = new List<GameObject>();

        // Get all root objects in the scene
        Scene activeScene = SceneManager.GetActiveScene();
        List<GameObject> sceneRoots = new List<GameObject>();
        activeScene.GetRootGameObjects(sceneRoots);

        // Add DontDestroyOnLoad objects (find them indirectly)
        List<GameObject> dontDestroyObjects = new List<GameObject>();

        // Create a temporary scene to help identify DontDestroyOnLoad objects
        GameObject temp = new GameObject();
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        // Find all GameObjects in all scenes
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.scene == dontDestroyScene && obj.transform.parent == null)
            {
                dontDestroyObjects.Add(obj);
            }
        }

        rootObjects.AddRange(sceneRoots);
        rootObjects.AddRange(dontDestroyObjects);

        return rootObjects.ToArray();
    }

    private void VisualizeGameObject(GameObject obj, StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);

        // Only proceed if the object is active or we want to show inactive objects
        if (obj.activeSelf || showInactiveObjects)
        {
            string activeStatus = obj.activeSelf ? "✓" : "✗";
            RectTransform rt = obj.GetComponent<RectTransform>();

            if (rt != null)
            {
                Vector2 size = rt.rect.size;
                Vector2 pos = rt.anchoredPosition;

                sb.AppendLine($"{indent}[{activeStatus}] {obj.name} ({size.x:F0}x{size.y:F0}, pos: {pos.x:F0},{pos.y:F0})");
            }
            else
            {
                sb.AppendLine($"{indent}[{activeStatus}] {obj.name}");
            }

            // Show components of interest
            if (showComponentDetails)
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;

                    string componentName = component.GetType().Name;

                    // Skip common UI components to reduce clutter
                    if (ignoredComponents.Contains(componentName))
                        continue;

                    // Add details for specific component types
                    string details = GetComponentDetails(component);
                    sb.AppendLine($"{indent}  - {componentName}{details}");
                }
            }

            // Recursively process children
            foreach (Transform child in obj.transform)
            {
                VisualizeGameObject(child.gameObject, sb, depth + 1);
            }
        }
    }

    private string GetComponentDetails(Component component)
    {
        if (component == null) return "";

        try
        {
            // Handle specific component types
            if (component is Image)
            {
                Image image = component as Image;
                return $" (sprite: {(image.sprite ? image.sprite.name : "none")}, color: {ColorToHex(image.color)})";
            }
            else if (component is Text)
            {
                Text text = component as Text;
                string textContent = text.text;
                if (textContent.Length > 20)
                    textContent = textContent.Substring(0, 17) + "...";
                return $" (text: \"{textContent}\", font: {text.fontSize}pt)";
            }
            else if (component is TextMeshProUGUI)
            {
                TextMeshProUGUI text = component as TextMeshProUGUI;
                string textContent = text.text;
                if (textContent.Length > 20)
                    textContent = textContent.Substring(0, 17) + "...";
                return $" (text: \"{textContent}\", font: {text.fontSize}pt)";
            }
            else if (component is Button)
            {
                Button button = component as Button;
                bool interactable = button.interactable;
                return $" (interactable: {interactable}, listeners: {button.onClick.GetPersistentEventCount()})";
            }
            else if (component is InputField)
            {
                InputField input = component as InputField;
                bool interactable = input.interactable;
                string textContent = input.text;
                if (textContent.Length > 20)
                    textContent = textContent.Substring(0, 17) + "...";
                return $" (interactable: {interactable}, text: \"{textContent}\")";
            }
            else if (component is TMP_InputField)
            {
                TMP_InputField input = component as TMP_InputField;
                bool interactable = input.interactable;
                string textContent = input.text;
                if (textContent.Length > 20)
                    textContent = textContent.Substring(0, 17) + "...";
                return $" (interactable: {interactable}, text: \"{textContent}\")";
            }
            else if (component is Canvas)
            {
                Canvas canvas = component as Canvas;
                return $" (mode: {canvas.renderMode}, sort: {canvas.sortingOrder})";
            }
            else if (component is RawImage)
            {
                RawImage raw = component as RawImage;
                return $" (texture: {(raw.texture ? raw.texture.name : "none")})";
            }
        }
        catch (Exception ex)
        {
            return $" (error: {ex.Message})";
        }

        return "";
    }

    private string ColorToHex(Color color)
    {
        return $"#{Mathf.RoundToInt(color.r * 255):X2}{Mathf.RoundToInt(color.g * 255):X2}{Mathf.RoundToInt(color.b * 255):X2}";
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// Check if TextMeshPro is available in the project
    /// </summary>
    private bool IsTMProAvailable()
    {
        // This will compile if TMPro is properly imported
        try
        {
            // Check if the required TMPro types exist
            Type textType = typeof(TextMeshProUGUI);
            return textType != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Find any available font asset to use with TextMeshPro
    /// </summary>
    private TMP_FontAsset GetAvailableTMPFont()
    {
        // Try to find any TMP font in the project
        try
        {
            return Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
        }
        catch
        {
            // Ignore errors
            return null;
        }
    }

    /// <summary>
    /// Create the debug UI hierarchy using TextMeshPro
    /// </summary>
    private void CreateDebugUI()
    {
        try
        {
            // Check if TextMeshPro is available
            if (!IsTMProAvailable())
            {
                Debug.LogError("[UIHierarchyVisualizer] TextMeshPro is not available or imported. The visualizer may not work correctly.");
            }

            // Create canvas
            GameObject canvasObj = new GameObject("UIHierarchyDebugCanvas");
            debugCanvas = canvasObj.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            debugCanvas.sortingOrder = 32767; // Make sure it's on top

            // Add canvas scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add raycaster
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panel = panelObj.AddComponent<Image>();
            panel.color = backgroundColor;

            // Set panel size
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(1 - panelWidth, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Create title bar
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(panelObj.transform, false);
            var titleImage = titleBar.AddComponent<Image>();
            titleImage.color = new Color(0.1f, 0.1f, 0.2f, 1f);

            // Set title bar size
            RectTransform titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(0, -40);
            titleRect.offsetMax = new Vector2(0, 0);

            // Create title text (using TextMeshPro)
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titleBar.transform, false);
            titleText = titleTextObj.AddComponent<TextMeshProUGUI>();

            // Set text properties
            titleText.text = "UI HIERARCHY VISUALIZER";
            titleText.fontSize = fontSize + 2;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = textColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.textWrappingMode = TextWrappingModes.Normal;

            // Set text size and anchors
            RectTransform titleTextRect = titleText.rectTransform;
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;

            // Create scene info text
            GameObject sceneInfoObj = new GameObject("SceneInfo");
            sceneInfoObj.transform.SetParent(titleBar.transform, false);
            sceneInfoText = sceneInfoObj.AddComponent<TextMeshProUGUI>();
            sceneInfoText.text = "Scene: " + SceneManager.GetActiveScene().name;
            sceneInfoText.fontSize = fontSize - 2;
            sceneInfoText.color = new Color(0.7f, 0.7f, 1f);
            sceneInfoText.alignment = TextAlignmentOptions.Left;
            sceneInfoText.textWrappingMode = TextWrappingModes.Normal;

            // Set scene info position
            RectTransform sceneInfoRect = sceneInfoText.rectTransform;
            sceneInfoRect.anchorMin = new Vector2(0, 0);
            sceneInfoRect.anchorMax = new Vector2(1, 0);
            sceneInfoRect.pivot = new Vector2(0.5f, 0);
            sceneInfoRect.offsetMin = new Vector2(5, -20);
            sceneInfoRect.offsetMax = new Vector2(-5, 0);

            // Create info bar
            infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(panelObj.transform, false);
            var infoImage = infoPanel.AddComponent<Image>();
            infoImage.color = new Color(0.2f, 0.2f, 0.3f, 0.5f);

            // Set info bar size
            RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(0.5f, 1);
            infoRect.offsetMin = new Vector2(0, -80);
            infoRect.offsetMax = new Vector2(0, -40);

            // Create controls text
            GameObject controlsObj = new GameObject("ControlsText");
            controlsObj.transform.SetParent(infoPanel.transform, false);
            var controlsText = controlsObj.AddComponent<TextMeshProUGUI>();
            controlsText.text = $"{toggleKey}: Toggle | {refreshKey}: Refresh | {copyKey}: Copy | {closeKey}: Close";
            controlsText.fontSize = fontSize - 2;
            controlsText.color = new Color(0.8f, 0.8f, 0.8f);
            controlsText.alignment = TextAlignmentOptions.Center;
            controlsText.textWrappingMode = TextWrappingModes.Normal;

            // Set controls position
            RectTransform controlsRect = controlsText.rectTransform;
            controlsRect.anchorMin = Vector2.zero;
            controlsRect.anchorMax = Vector2.one;
            controlsRect.offsetMin = Vector2.zero;
            controlsRect.offsetMax = Vector2.zero;

            // Create pagination panel
            paginationPanel = new GameObject("PaginationPanel");
            paginationPanel.transform.SetParent(panelObj.transform, false);
            var paginationImage = paginationPanel.AddComponent<Image>();
            paginationImage.color = new Color(0.2f, 0.3f, 0.4f, 0.7f);

            // Set pagination panel size
            RectTransform paginationRect = paginationPanel.GetComponent<RectTransform>();
            paginationRect.anchorMin = new Vector2(0, 1);
            paginationRect.anchorMax = new Vector2(1, 1);
            paginationRect.pivot = new Vector2(0.5f, 1);
            paginationRect.offsetMin = new Vector2(0, -120);
            paginationRect.offsetMax = new Vector2(0, -80);

            // Create pagination info text
            GameObject paginationTextObj = new GameObject("PaginationText");
            paginationTextObj.transform.SetParent(paginationPanel.transform, false);
            paginationInfoText = paginationTextObj.AddComponent<TextMeshProUGUI>();
            paginationInfoText.text = $"Page 1 of 1 | {prevPageKey} Prev | {nextPageKey} Next";
            paginationInfoText.fontSize = fontSize - 1;
            paginationInfoText.color = new Color(1f, 1f, 1f);
            paginationInfoText.alignment = TextAlignmentOptions.Center;
            paginationInfoText.textWrappingMode = TextWrappingModes.Normal;

            // Set pagination text position
            RectTransform paginationTextRect = paginationInfoText.rectTransform;
            paginationTextRect.anchorMin = Vector2.zero;
            paginationTextRect.anchorMax = Vector2.one;
            paginationTextRect.offsetMin = Vector2.zero;
            paginationTextRect.offsetMax = Vector2.zero;

            // Create notification panel
            notificationPanel = new GameObject("NotificationPanel");
            notificationPanel.transform.SetParent(panelObj.transform, false);
            var notifImage = notificationPanel.AddComponent<Image>();
            notifImage.color = new Color(0.2f, 0.6f, 0.2f, 0.8f);

            // Set notification panel size
            RectTransform notifRect = notificationPanel.GetComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 0);
            notifRect.anchorMax = new Vector2(0.5f, 0);
            notifRect.pivot = new Vector2(0.5f, 0);
            notifRect.sizeDelta = new Vector2(200, 40);
            notifRect.anchoredPosition = new Vector2(0, 50);

            // Create notification text
            GameObject notifTextObj = new GameObject("NotificationText");
            notifTextObj.transform.SetParent(notificationPanel.transform, false);
            notificationText = notifTextObj.AddComponent<TextMeshProUGUI>();
            notificationText.text = "Notification";
            notificationText.fontSize = fontSize;
            notificationText.fontStyle = FontStyles.Bold;
            notificationText.color = Color.white;
            notificationText.alignment = TextAlignmentOptions.Center;
            notificationText.textWrappingMode = TextWrappingModes.Normal;

            // Set notification text position
            RectTransform notifTextRect = notificationText.rectTransform;
            notifTextRect.anchorMin = Vector2.zero;
            notifTextRect.anchorMax = Vector2.one;
            notifTextRect.offsetMin = Vector2.zero;
            notifTextRect.offsetMax = Vector2.zero;

            // Hide notification initially
            notificationPanel.SetActive(false);

            // Create scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

            // Set scroll view size
            RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.offsetMin = new Vector2(10, 10);
            scrollRectTransform.offsetMax = new Vector2(-10, -120); // More space at top for pagination

            // Create viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.2f);
            var viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Set viewport size
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Create content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            // Set content size
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, 0);

            // Create text component (using TextMeshPro)
            GameObject textObj = new GameObject("HierarchyText");
            textObj.transform.SetParent(contentObj.transform, false);

            // Set text properties
            outputText = textObj.AddComponent<TextMeshProUGUI>();
            outputText.fontSize = fontSize;
            outputText.color = textColor;
            outputText.textWrappingMode = TextWrappingModes.Normal;
            outputText.isTextObjectScaleStatic = false; // Better handling of large text

            // Set text size and anchors
            RectTransform textRect = outputText.rectTransform;
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);

            // Hook up the scroll view
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 15;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Add content size fitter
            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add layout element to text
            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;

            // Add vertical layout group to content
            VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.spacing = 0;

            // Store reference
            debugCanvas = canvasObj.GetComponent<Canvas>();

            // Set the canvas to be a child of this object
            canvasObj.transform.SetParent(transform);

            Debug.Log("[UIHierarchyVisualizer] Debug UI created successfully. " +
                      $"Press {toggleKey} to show, {nextPageKey}/{prevPageKey} to navigate pages, {copyKey} to copy to clipboard.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIHierarchyVisualizer] Error creating debug UI: {ex.Message}");
        }
    }
}