using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace LoginSystem
{
    // Custom certificate handler to bypass SSL validation
    public class AcceptAllCertificatesSignedHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public class LoginRegisterManager : MonoBehaviour
    {
        [SerializeField] private GameObject canvas;
        public static LoginRegisterManager Instance { get; private set; }

        [Header("API Settings")]
        [SerializeField] private TMP_InputField serverIpInput;
        [SerializeField] private string defaultServerPort = "7170";
        [SerializeField] private bool useHttps = true;
        [SerializeField] private bool bypassSslValidation = true;
        [SerializeField] private float requestTimeout = 10f;

        [Header("Panel References")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject topPlayersPanel;

        [Header("Login Panel Components")]
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button gotoRegisterButton;
        [SerializeField] private Button gotoTopPlayerButton;

        [Header("Register Panel Components")]
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField confirmPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button gotoLoginButton;

        [Header("Top Players Panel Components")]
        [SerializeField] private Transform topPlayersListParent;
        [SerializeField] private Button backFromTopPlayersButton;
        [SerializeField] private Button refreshTopPlayersButton;

        [Header("UI Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Animation Settings")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float animationDuration = 0.3f;

        private string gameScenePath = "SampleScene1";
        private GameObject previousPanel;
        private bool wasServerInputVisible = true;

        private void Awake()
        {
            // Implement singleton pattern and don't destroy on load
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Subscribe to scene change events
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                // Disable all canvas children except those tagged with "LoginPanel"
                SetupInitialCanvasState();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Bypass SSL certificate validation for development
            if (bypassSslValidation)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            
            InitializeUI();
            SetupButtonListeners();
        }

        private void Update()
        {
            // Toggle leaderboard when Escape is pressed
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsTopPlayersPanelActive())
                {
                    // If already showing top players panel, hide it only
                    HideTopPlayersPanel();
                }
                else
                {
                    // Show top players panel
                    ShowTopPlayersPanel();
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Hide server input field and canvas children in non-login scenes
            if (scene.name != "LoginScence")
            {
                if (serverIpInput != null)
                {
                    serverIpInput.gameObject.SetActive(false);
                }
                
                // Disable all children of the canvas
                if (canvas != null)
                {
                    SetCanvasChildrenActive(false);
                }
            }
            else if (scene.name == "LoginScence")
            {
                if (serverIpInput != null)
                {
                    serverIpInput.gameObject.SetActive(true);
                }
                
                // Restore initial canvas state (only LoginPanel tagged objects)
                if (canvas != null)
                {
                    SetupInitialCanvasState();
                }
            }
        }

        private void InitializeUI()
        {
            ShowLoginPanel();
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
            
            // Set default IP if not already set
            if (serverIpInput != null && string.IsNullOrEmpty(serverIpInput.text))
            {
                serverIpInput.text = "localhost";
            }
            
            // Make sure server input persists across scenes
            if (serverIpInput != null)
            {
                DontDestroyOnLoad(serverIpInput.transform.root.gameObject);
            }
        }

        private string GetApiBaseUrl()
        {
            string serverIp = serverIpInput?.text ?? "localhost";
            if (string.IsNullOrEmpty(serverIp.Trim()))
            {
                serverIp = "localhost";
            }
            string protocol = useHttps ? "https" : "http";
            return $"{protocol}://{serverIp.Trim()}:{defaultServerPort}";
        }

        private void SetupButtonListeners()
        {
            // Login panel buttons
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);

            if (gotoRegisterButton != null)
                gotoRegisterButton.onClick.AddListener(ShowRegisterPanel);

            if (gotoTopPlayerButton != null)
                gotoTopPlayerButton.onClick.AddListener(ShowTopPlayersPanel);

            // Register panel buttons
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);

            if (gotoLoginButton != null)
                gotoLoginButton.onClick.AddListener(ShowLoginPanel);

            // Top players panel buttons
            if (backFromTopPlayersButton != null)
                backFromTopPlayersButton.onClick.AddListener(BackFromTopPlayers);

            if (refreshTopPlayersButton != null)
                refreshTopPlayersButton.onClick.AddListener(RefreshTopPlayers);
        }

        public void ShowLoginPanel()
        {
            SetActivePanel(loginPanel);
            ClearInputFields(true);
            HideFeedback();
        }

        public void ShowRegisterPanel()
        {
            SetActivePanel(registerPanel);
            ClearInputFields(false);
            HideFeedback();
        }

        public void ShowTopPlayersPanel()
        {
            previousPanel = GetCurrentActivePanel();
            SetActivePanel(topPlayersPanel);
            HideFeedback();
            LoadTopPlayers();
        }

        public void HideTopPlayersPanel()
        {
            if (topPlayersPanel != null)
            {
                topPlayersPanel.SetActive(false);
                HideFeedback();
                Debug.Log("[LoginRegisterManager] Top players panel hidden");
            }
        }

        private void BackFromTopPlayers()
        {
            if (previousPanel != null)
            {
                SetActivePanel(previousPanel);
            }
            else
            {
                ShowLoginPanel();
            }
            HideFeedback();
        }

        private void RefreshTopPlayers()
        {
            LoadTopPlayers();
        }

        private GameObject GetCurrentActivePanel()
        {
            if (loginPanel != null && loginPanel.activeInHierarchy)
                return loginPanel;
            if (registerPanel != null && registerPanel.activeInHierarchy)
                return registerPanel;
            if (topPlayersPanel != null && topPlayersPanel.activeInHierarchy)
                return topPlayersPanel;
            return loginPanel; // Default
        }

        private void SetActivePanel(GameObject targetPanel)
        {
            if (useAnimation)
            {
                StartCoroutine(SwitchPanelWithAnimation(GetCurrentActivePanel(), targetPanel));
            }
            else
            {
                SetPanelActive(loginPanel, targetPanel == loginPanel);
                SetPanelActive(registerPanel, targetPanel == registerPanel);
                SetPanelActive(topPlayersPanel, targetPanel == topPlayersPanel);
            }
        }

        private System.Collections.IEnumerator SwitchPanelWithAnimation(GameObject panelToHide, GameObject panelToShow)
        {
            if (panelToHide != null && panelToHide.activeInHierarchy && panelToHide != panelToShow)
            {
                CanvasGroup hideGroup = GetOrAddCanvasGroup(panelToHide);
                yield return StartCoroutine(FadePanel(hideGroup, 1f, 0f));
                panelToHide.SetActive(false);
            }

            if (panelToShow != null)
            {
                panelToShow.SetActive(true);
                CanvasGroup showGroup = GetOrAddCanvasGroup(panelToShow);
                yield return StartCoroutine(FadePanel(showGroup, 0f, 1f));
            }
        }

        private System.Collections.IEnumerator FadePanel(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
        {
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = obj.AddComponent<CanvasGroup>();
            }
            return canvasGroup;
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private void OnLoginButtonClicked()
        {
            string username = loginUsernameInput?.text ?? "";
            string password = loginPasswordInput?.text ?? "";

            if (ValidateLoginInput(username, password))
            {
                ProcessLogin(username, password);
            }
        }

        private void OnRegisterButtonClicked()
        {
            string username = registerUsernameInput?.text ?? "";
            string password = registerPasswordInput?.text ?? "";
            string confirmPassword = confirmPasswordInput?.text ?? "";

            if (ValidateRegisterInput(username, password, confirmPassword))
            {
                ProcessRegister(username, password);
            }
        }

        private bool ValidateLoginInput(string username, string password)
        {
            if (!ValidateServerIp())
                return false;

            if (string.IsNullOrEmpty(username))
            {
                ShowFeedback("Please enter username!", Color.red);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowFeedback("Please enter password!", Color.red);
                return false;
            }

            return true;
        }

        private bool ValidateRegisterInput(string username, string password, string confirmPassword)
        {
            if (!ValidateServerIp())
                return false;

            if (string.IsNullOrEmpty(username))
            {
                ShowFeedback("Please enter username!", Color.red);
                return false;
            }

            if (username.Length < 3)
            {
                ShowFeedback("Username must be at least 3 characters!", Color.red);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowFeedback("Please enter password!", Color.red);
                return false;
            }

            if (password.Length < 2)
            {
                ShowFeedback("Password must be at least 2 characters!", Color.red);
                return false;
            }

            if (password != confirmPassword)
            {
                ShowFeedback("Confirmation password does not match!", Color.red);
                return false;
            }

            return true;
        }

        private bool ValidateServerIp()
        {
            string serverIp = serverIpInput?.text ?? "";
            if (string.IsNullOrEmpty(serverIp.Trim()))
            {
                ShowFeedback("Please enter server IP address!", Color.red);
                return false;
            }
            return true;
        }

        private void ProcessLogin(string username, string password)
        {
            ShowFeedback("Loging...", Color.yellow);
            StartCoroutine(LoginCoroutine(username, password));
        }

        private void ProcessRegister(string username, string password)
        {
            ShowFeedback("Registering...", Color.yellow);
            StartCoroutine(RegisterCoroutine(username, password));
        }

        private IEnumerator LoginCoroutine(string username, string password)
        {
            string loginUrl = $"{GetApiBaseUrl()}/api/Auth/login";

            LoginRequest loginData = new LoginRequest
            {
                username = username,
                password = password
            };

            string jsonData = JsonUtility.ToJson(loginData);

            using (UnityWebRequest request = new UnityWebRequest(loginUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)requestTimeout;
                
                // Add certificate handler to bypass SSL validation
                if (bypassSslValidation)
                {
                    request.certificateHandler = new AcceptAllCertificatesSignedHandler();
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Login response: {responseText}");

                    try
                    {
                        ApiResponse response = JsonUtility.FromJson<ApiResponse>(responseText);

                        if (response.success)
                        {
                            OnLoginSuccess(response, username);
                        }
                        else
                        {
                            OnLoginFailure(response.message);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing login response: {e.Message}");
                        OnLoginFailure("Error processing response from server");
                    }
                }
                else
                {
                    string errorMessage = $"Connection error: {request.error}";
                    Debug.LogError($"Login request failed: {errorMessage}");
                    OnLoginFailure(errorMessage);
                }
            }
        }

        private IEnumerator RegisterCoroutine(string username, string password)
        {
            string registerUrl = $"{GetApiBaseUrl()}/api/Auth/register";

            RegisterRequest registerData = new RegisterRequest
            {
                username = username,
                password = password
            };

            string jsonData = JsonUtility.ToJson(registerData);

            using (UnityWebRequest request = new UnityWebRequest(registerUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)requestTimeout;
                
                // Add certificate handler to bypass SSL validation
                if (bypassSslValidation)
                {
                    request.certificateHandler = new AcceptAllCertificatesSignedHandler();
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Register response: {responseText}");

                    try
                    {
                        ApiResponse response = JsonUtility.FromJson<ApiResponse>(responseText);

                        if (response.success)
                        {
                            OnRegisterSuccess(response, username);
                        }
                        else
                        {
                            OnRegisterFailure(response.message);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing register response: {e.Message}");
                        OnRegisterFailure("Error processing response from server");
                    }
                }
                else
                {
                    string errorMessage = $"Connection error: {request.error}";
                    Debug.LogError($"Register request failed: {errorMessage}");
                    OnRegisterFailure(errorMessage);
                }
            }
        }

        private void LoadTopPlayers()
        {
            ShowFeedback("Loading top players...", Color.yellow);

            // TEST: Fake data
            //CreateTestItems();

            // Call api to get top players
             StartCoroutine(GetTopPlayersCoroutine());
        }

        // Method test để tạo fake items
        private void CreateTestItems()
        {
            UserData[] testData = new UserData[]
            {
                new UserData { id = 1, username = "TestUser1", score = 100, isEnable = true },
                new UserData { id = 2, username = "TestUser2", score = 90, isEnable = true },
                new UserData { id = 3, username = "TestUser3", score = 80, isEnable = true }
            };

            DisplayTopPlayers(testData);
            HideFeedback();
        }

        private IEnumerator GetTopPlayersCoroutine()
        {
            string topPlayersUrl = $"{GetApiBaseUrl()}/api/User/top10-user";

            using (UnityWebRequest request = UnityWebRequest.Get(topPlayersUrl))
            {
                request.timeout = (int)requestTimeout;
                
                // Add certificate handler to bypass SSL validation
                if (bypassSslValidation)
                {
                    request.certificateHandler = new AcceptAllCertificatesSignedHandler();
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Top players response: {responseText}");

                    try
                    {
                        // Parse array of users
                        UserData[] topPlayers = JsonHelper.FromJson<UserData>(responseText);
                        DisplayTopPlayers(topPlayers);
                        HideFeedback();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing top players response: {e.Message}");
                        ShowFeedback("Error loading top players", Color.red);
                    }
                }
                else
                {
                    string errorMessage = $"Connection error: {request.error}";
                    Debug.LogError($"Top players request failed: {errorMessage}");
                    ShowFeedback("Failed to load top players", Color.red);
                }
            }
        }

        private void DisplayTopPlayers(UserData[] topPlayers)
        {
            Debug.Log($"DisplayTopPlayers called with {topPlayers?.Length ?? 0} players");

            if (topPlayersListParent == null)
            {
                Debug.LogError("topPlayersListParent is null! Please assign Content of ScrollView.");
                ShowFeedback("UI Setup Error: Missing Content reference", Color.red);
                return;
            }

            int childCount = topPlayersListParent.childCount;
            Debug.Log($"Clearing {childCount} existing children");

            for (int i = childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(topPlayersListParent.GetChild(i).gameObject);
            }

            if (topPlayers != null && topPlayers.Length > 0)
            {
                Debug.Log($"Creating {topPlayers.Length} top player items dynamically");

                for (int i = 0; i < topPlayers.Length; i++)
                {
                    CreatePlayerItemDynamic(i + 1, topPlayers[i].username, topPlayers[i].score);
                }

                Debug.Log($"Finished creating {topPlayers.Length} items");
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(topPlayersListParent.GetComponent<RectTransform>());
            }
            else
            {
                Debug.LogWarning("No top players data to display");
                ShowFeedback("No players found", Color.yellow);
            }
        }

        private void CreatePlayerItemDynamic(int rank, string username, float score)
        {
            GameObject itemContainer = new GameObject($"PlayerItem_{rank}_{username}");
            itemContainer.transform.SetParent(topPlayersListParent, false);

            RectTransform itemRect = itemContainer.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 40f);
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image bgImage = itemContainer.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            UnityEngine.UI.HorizontalLayoutGroup layoutGroup = itemContainer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft; // Changed to MiddleLeft
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(15, 15, 10, 10);

            CreateTextElementFixed(itemContainer.transform, "RankText", $"#{rank}", 50f, GetRankColor(rank), TextAlignmentOptions.Center);
            CreateTextElementFlexible(itemContainer.transform, "UsernameText", username, Color.white, TextAlignmentOptions.Left);
            CreateTextElementFixed(itemContainer.transform, "ScoreText", score.ToString("F0"), 40f, Color.yellow, TextAlignmentOptions.Right);
            Debug.Log($"Created dynamic item: #{rank} - {username} - {score}");
        }

        private GameObject CreateTextElementFixed(Transform parent, string name, string text, float width, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(width, 40f); // Fixed size
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(0, 1);
            textRect.pivot = new Vector2(0, 0.5f);

            // Add TextMeshPro component
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 18f;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.enableAutoSizing = false;

            // Add Layout Element với fixed size
            UnityEngine.UI.LayoutElement layoutElement = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 40f;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            return textObj;
        }

        private GameObject CreateTextElementFlexible(Transform parent, string name, string text, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200f, 40f); // Default size
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 0.5f);

            // Add TextMeshPro component
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 18f;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.fontStyle = FontStyles.Normal;
            textComponent.enableAutoSizing = false;

            // Add Layout Element với flexible width
            UnityEngine.UI.LayoutElement layoutElement = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.minWidth = 150f;
            layoutElement.preferredHeight = 40f;
            layoutElement.flexibleWidth = 1f; // Take remaining space
            layoutElement.flexibleHeight = 0;

            return textObj;
        }

        private Color GetRankColor(int rank)
        {
            switch (rank)
            {
                case 1: return new Color(1f, 0.84f, 0f);        // Gold
                case 2: return new Color(0.75f, 0.75f, 0.75f);  // Silver  
                case 3: return new Color(0.8f, 0.5f, 0.2f);     // Bronze
                default: return new Color(0.9f, 0.9f, 0.9f);    // Light gray
            }
        }

        private void SetupTopPlayerItemManually(GameObject item, int rank, string username, float score)
        {
            Debug.Log($"Manual setup for item: {item.name}");

            // Tìm tất cả TextMeshPro components
            TextMeshProUGUI[] allTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
            Debug.Log($"Found {allTexts.Length} TextMeshPro components");

            if (allTexts.Length >= 3)
            {
                allTexts[0].text = $"#{rank}";
                allTexts[1].text = username;
                allTexts[2].text = score.ToString("F0");
            }
        }

        private void OnLoginSuccess(ApiResponse response, string username)
        {
            ShowFeedback(response.message, Color.green);

            if (response.user != null)
            {
                PlayerPrefs.SetString("UserId", response.user.id.ToString());
                PlayerPrefs.SetString("Username", response.user.username);
                PlayerPrefs.SetInt("UserScore", response.user.score);
                PlayerPrefs.SetInt("IsUserEnabled", response.user.isEnable ? 1 : 0);
                PlayerPrefs.Save();

                Debug.Log($"User info saved - ID: {response.user.id}, Username: {response.user.username}, Score: {response.user.score}");
            }

            // Go to game scene
            SceneManager.LoadScene(gameScenePath);

            OnUserLoggedIn?.Invoke(username, response);
        }

        private void OnLoginFailure(string errorMessage)
        {
            ShowFeedback(errorMessage, Color.red);
        }

        private void OnRegisterSuccess(ApiResponse response, string username)
        {
            ShowFeedback(response.message, Color.green);
            OnUserRegistered?.Invoke(username);
            StartCoroutine(DelayedSwitchToLogin());
        }

        private void OnRegisterFailure(string errorMessage)
        {
            ShowFeedback(errorMessage, Color.red);
        }

        private IEnumerator DelayedSwitchToLogin()
        {
            yield return new WaitForSeconds(1.5f);
            ShowLoginPanel();
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);
            }
        }

        private void HideFeedback()
        {
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }

        private void ClearInputFields(bool isLoginPanel)
        {
            if (isLoginPanel)
            {
                if (loginUsernameInput != null) loginUsernameInput.text = "";
                if (loginPasswordInput != null) loginPasswordInput.text = "";
            }
            else
            {
                if (registerUsernameInput != null) registerUsernameInput.text = "";
                if (registerPasswordInput != null) registerPasswordInput.text = "";
                if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            }
        }

        // Events
        public System.Action<string, ApiResponse> OnUserLoggedIn;
        public System.Action<string> OnUserRegistered;

        // Public methods for external access
        public bool IsLoginPanelActive()
        {
            return loginPanel != null && loginPanel.activeInHierarchy;
        }

        public bool IsRegisterPanelActive()
        {
            return registerPanel != null && registerPanel.activeInHierarchy;
        }

        public bool IsTopPlayersPanelActive()
        {
            return topPlayersPanel != null && topPlayersPanel.activeInHierarchy;
        }

        public void TogglePanel()
        {
            if (IsLoginPanelActive())
                ShowRegisterPanel();
            else
                ShowLoginPanel();
        }

        // Utility methods
        public static string GetSavedUserId()
        {
            return PlayerPrefs.GetString("UserId", "");
        }

        public static string GetSavedUsername()
        {
            return PlayerPrefs.GetString("Username", "");
        }

        public static int GetSavedUserScore()
        {
            return PlayerPrefs.GetInt("UserScore", 0);
        }

        public static bool IsUserEnabled()
        {
            return PlayerPrefs.GetInt("IsUserEnabled", 0) == 1;
        }

        public static bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(GetSavedUserId()) && !string.IsNullOrEmpty(GetSavedUsername());
        }

        public static void Logout()
        {
            PlayerPrefs.DeleteKey("UserId");
            PlayerPrefs.DeleteKey("Username");
            PlayerPrefs.DeleteKey("UserScore");
            PlayerPrefs.DeleteKey("IsUserEnabled");
            PlayerPrefs.Save();
        }

        public static UserData GetCurrentUser()
        {
            if (IsUserLoggedIn())
            {
                return new UserData
                {
                    id = int.Parse(GetSavedUserId()),
                    username = GetSavedUsername(),
                    score = GetSavedUserScore(),
                    isEnable = IsUserEnabled()
                };
            }
            return null;
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene events to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Public method to show/hide server input manually if needed
        public void SetServerInputVisibility(bool visible)
        {
            if (serverIpInput != null)
            {
                serverIpInput.gameObject.SetActive(visible);
            }
        }

        // Setup initial canvas state - disable all children except LoginPanel tagged ones
        private void SetupInitialCanvasState()
        {
            if (canvas == null) return;

            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                
                // Keep LoginPanel tagged objects active, disable others
                if (child.gameObject.CompareTag("LoginPanel"))
                {
                    child.gameObject.SetActive(true);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // Method to enable/disable all children of the canvas
        private void SetCanvasChildrenActive(bool active)
        {
            if (canvas == null) return;

            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                child.gameObject.SetActive(active);
            }
        }

        // Public method to manually control canvas visibility
        public void SetCanvasVisibility(bool visible)
        {
            SetCanvasChildrenActive(visible);
        }

        // Public method to get server IP for other scripts
        public string GetServerIP()
        {
            Debug.Log($"[LoginRegisterManager] GetServerIP called");
            
            if (serverIpInput == null)
            {
                Debug.LogWarning("[LoginRegisterManager] serverIpInput is NULL! Make sure it's assigned in Inspector.");
                return "localhost";
            }
            
            Debug.Log($"[LoginRegisterManager] serverIpInput found, text: '{serverIpInput.text}'");
            Debug.Log($"[LoginRegisterManager] serverIpInput active: {serverIpInput.gameObject.activeInHierarchy}");
            
            if (!string.IsNullOrEmpty(serverIpInput.text))
            {
                string trimmedIP = serverIpInput.text.Trim();
                Debug.Log($"[LoginRegisterManager] Returning IP: '{trimmedIP}'");
                return trimmedIP;
            }
            
            Debug.LogWarning("[LoginRegisterManager] serverIpInput text is empty, returning localhost");
            return "localhost"; // Default fallback
        }
    }

    // Helper class for JSON array parsing
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrappedJson = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper.Items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    // Data classes for API communication
    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public bool success;
        public string message;
        public UserData user;
    }

    [System.Serializable]
    public class UserData
    {
        public int id;
        public string username;
        public int score;
        public bool isEnable;
    }
}
