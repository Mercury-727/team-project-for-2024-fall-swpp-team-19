using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{

    public TMP_Text titleText;
    public Color[] colors;
    private int colorIndex = 0;
    public GameObject gameButtonGroup;
    public GameObject hostInputField;
    public GameObject controlButtonGroup;
    public GameObject backButton;
    public GameObject backGround;
    public GameObject undoButton;

    private TMP_InputField inputField;

    private bool isConnecting = false;


    [Header("Network Settings")]
    [Tooltip("The name of the Room Scene (Lobby Scene).")]
    [SerializeField] private string roomSceneName = "LobbyScene";

    void Start()
    {
        StartCoroutine(ChangeTitleColor());
        gameButtonGroup.SetActive(false);
        hostInputField.SetActive(false);

        inputField = hostInputField.GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnEndEditInputField);
        }
        

        NetworkClient.OnConnectedEvent += OnClientConnected;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
    }

    
    private void LoadLobbyScene()
    {
        if (NetworkManager.singleton != null)
        {
            Debug.Log("Loading Lobby Scene...");
            SceneManager.LoadScene(roomSceneName); // Load the Lobby Scene
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }

    public void PlayGameButton()
    {
        gameButtonGroup.SetActive(true);
        controlButtonGroup.SetActive(false);
        hostInputField.SetActive(false);
        backButton.SetActive(false);
        // backGround.SetActive(true);
    }

    public void UndoButton()
    {
        gameButtonGroup.SetActive(false);
        controlButtonGroup.SetActive(true);
        hostInputField.SetActive(false);
        backButton.SetActive(false);
        backGround.SetActive(false);
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void CreateLobbyButton()
    {
        NetworkManager.singleton.StartHost();
        LoadLobbyScene();
    }

    public void JoinButton()
    {
        hostInputField.SetActive(true);
        gameButtonGroup.SetActive(false);
        backButton.SetActive(true);
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    private void OnEndEditInputField(string roomCode)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConnectToServer();
        }
    }

    public void ConnectToServer()
    {
        if (!isConnecting)
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                string roomCode = inputField.text;
                CustomRoomManager.Instance.SetRoomCode(roomCode);

                if (roomCode.ToLower() == "localhost")
                {
                    Debug.Log("Connecting to localhost...");
                    CustomRoomManager.Instance.networkAddress = "127.0.0.1";
                    CustomRoomManager.Instance.StartClient();
                    isConnecting = true;

                    Debug.Log("Attempting to connect to localhost...");
                    LoadLobbyScene();
                    return;
                }

                string decoded = DecodeRoomCode(roomCode);

                if (!string.IsNullOrEmpty(decoded))
                {
                    string[] splitData = decoded.Split(':');
                    if (splitData.Length == 2)
                    {
                        string ipAddress = splitData[0];
                        int port;
                        if (int.TryParse(splitData[1], out port) && port >= 0 && port <= 65535)
                        {
                            Debug.Log($"Attempting to connect to IP: {ipAddress}, Port: {port}");

                            CustomRoomManager.Instance.networkAddress = ipAddress;

                            var transport = CustomRoomManager.Instance.GetComponent<Mirror.TelepathyTransport>();
                            if (transport != null)
                            {
                                transport.port = (ushort)port;
                            }

                            isConnecting = true;
                            CustomRoomManager.Instance.StartClient();
                            LoadLobbyScene();
                            Debug.Log("Checking room existence...");
                        }
                        else
                        {
                            Debug.LogError("Invalid port in room code.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Invalid room code format.");
                    }
                }
                else
                {
                    Debug.LogError("Failed to decode room code.");
                }
            }
            else
            {
                Debug.LogWarning("Please enter a valid room code.");
            }
        }
        else if (isConnecting)
        {
            Debug.LogWarning("Already attempting to connect...");
        }
        else
        {
            Debug.LogError("NetworkManager is not found.");
        }
    }

    private void OnClientConnected()
    {
        Debug.Log("Client successfully connected to the server.");
        isConnecting = false;


    }

    private void OnClientDisconnected()
    {
        Debug.LogError("Failed to connect to the server or disconnected.");
        isConnecting = false;
    }

    private string DecodeRoomCode(string roomCode)
    {
        try
        {
            byte[] data = System.Convert.FromBase64String(roomCode);
            return System.Text.Encoding.UTF8.GetString(data);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error decoding room code: {ex.Message}");
            return null;
        }
    }

    private IEnumerator ChangeTitleColor()
    {
        Color startColor = titleText.color;
        Color endColor = GetColor(colorIndex);
        
        float duration = 1f; // 색상이 전환되는 데 걸리는 시간

        while (true)
        {
            colorIndex = (colorIndex + 1) % colors.Length;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                titleText.color = Color.Lerp(startColor, endColor, elapsed / duration);
                yield return null;
            }
            startColor = endColor;
            endColor = GetColor(colorIndex);
        }
    }

    private Color GetColor(int index)
    {
        return colors[index];
    }
}
