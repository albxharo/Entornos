using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StartGameVariables : NetworkBehaviour
{

    private bool partidaIniciada = false;
    private bool gameModeChosen = false;

    // Número de humanos y zombis permitidos
    int numHumans;
    int numZombies;

    // Listas de jugadores sincronizadas en red
    public NetworkList<ulong> humanList;
    public NetworkList<ulong> zombieList;
    private NetworkList<ulong> readyPlayersList;
    public NetworkList<FixedString4096Bytes> playersNames;

    public float coinsDensity = 20f;
    public float minutes = 5f; 


    public GameObject readycanvas;


    [SerializeField] private GameObject panelNumJugadores;
    [SerializeField] private TMP_InputField inputFieldNumJugadores;


    public NetworkVariable<int> numJugadores = new NetworkVariable<int>(
         0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Último tipo de jugador asignado (0: zombie, 1: humano)
    public NetworkVariable<int> lastTypePlayer =
        new NetworkVariable<int>(
             1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Sincroniza el modo de juego a todos los clientes
    public NetworkVariable<GameMode> SelectedGameMode =
        new NetworkVariable<GameMode>(
             GameMode.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Numero total de jugadores
    public NetworkVariable<int> numTotalPlayers =
        new NetworkVariable<int>(
             1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public List<ulong> jugadoresPendientes = new List<ulong>();
    public static StartGameVariables Instance { get; private set; }


    private void Awake()
    {
        // Configurar Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre escenas
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Elimina duplicados
        }

        // Aquí inicializamos TODAS las NetworkList ANTES de cualquier spawn:
        humanList = new NetworkList<ulong>();
        zombieList = new NetworkList<ulong>();
        readyPlayersList = new NetworkList<ulong>();
        playersNames = new NetworkList<FixedString4096Bytes>();
    }

    public void SetNumPlayers()
    {
        Debug.Log("Recibido el input field");
        if (IsHost)
        {
            string texto = inputFieldNumJugadores.text;

            if (int.TryParse(texto, out int numero) && numero > 0)
            {
                SetNumPlayersRpc(numero);
                panelNumJugadores.SetActive(false);
                readycanvas.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Entrada inválida. Asegúrate de introducir un número.");
            }
        }
    }

    private void SetNumPlayersRpc(int numero)
    {
        numJugadores.Value = numero;
    }

    // Asigna equipo al nuevo cliente y lo añade a la lista de espera
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || partidaIniciada)
            return;
        numTotalPlayers.Value++;
        string equipo = AsignarEquipo(clientId);
        //GuardarNombre();

        if (equipo == "ninguno")
        {
            if (lastTypePlayer.Value == 0)
            {
                Debug.Log("Ultimo fue: zombie");
                lastTypePlayer.Value = 1;
                equipo = "human";
                humanList.Add(clientId);

            }
            else
            {
                equipo = "zombie";
                lastTypePlayer.Value = 0;
                zombieList.Add(clientId);

                Debug.Log("Ultimo fue: humano");

            }
        }

        Debug.Log($"Jugador {clientId} asignado a {equipo}, esperando inicio de partida");

        jugadoresPendientes.Add(clientId);

        // Tras asignar equipos, intentamos arrancar partida
        TryStartMatchIfReady();
    }

    private void TryStartMatchIfReady()
    {
        // Sólo lanzar IniciarPartida cuando
        //  1) Teams completos, y
        //  2) El Host YA ha elegido modo
        if (gameModeChosen
            && readyPlayersList.Count > (numJugadores.Value)
            && !partidaIniciada)
        {
            for (int i = 0; i < playersNames.Count; i++)
            {
                Debug.Log($"Nombre {i}: {playersNames[i].ToString()}");
            }
            // Avisamos a todos los clientes de que empiece el juego!*/
            NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }


    // Asigna equipo al jugador dependiendo del estado actual
    private string AsignarEquipo(ulong clientId)
    {
        if (humanList.Count >= numHumans && zombieList.Count >= numZombies)
        {
            if (humanList.Count == numHumans && zombieList.Count == numZombies)
            {
                Debug.Log("Equipos recién completados");
                lastTypePlayer.Value = 1; // Para que el siguiente sea zombie
                return "ninguno";

            }
            Debug.Log("Equipos completados");
            return "ninguno";
        }

        if (humanList.Count >= numHumans)
        {
            zombieList.Add(clientId);
            lastTypePlayer.Value = 0;
            return "zombie";
        }

        if (zombieList.Count >= numZombies)
        {
            humanList.Add(clientId);
            lastTypePlayer.Value = 1;

            return "human";
        }

        // Asignación aleatoria si hay espacio en ambos equipos
        if (UnityEngine.Random.value < 0.5f)
        {
            humanList.Add(clientId);
            lastTypePlayer.Value = 1;

            return "human";
        }
        else
        {
            zombieList.Add(clientId);
            lastTypePlayer.Value = 0;

            return "zombie";
        }
    }
    public void GuardarNombre(string nickname)
    {
        GuardarNombreServerRpc(nickname);
    }


    [ServerRpc(RequireOwnership = false)]
    public void GuardarNombreServerRpc(string nickname, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Rellenar con strings vacíos hasta alcanzar el índice `clientId`
        while (playersNames.Count <= (int)clientId)
        {
            playersNames.Add(new FixedString4096Bytes(""));  // Espacios vacíos
        }

        playersNames[(int)clientId] = new FixedString4096Bytes(nickname);

        Debug.Log($"[Server] Jugador {clientId} registrado como '{nickname}' en índice {clientId}");
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //NetworkManager.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;

            // Suscribimos un listener para cuando el servidor cambie el modo
            SelectedGameMode.OnValueChanged += (oldMode, newMode) =>
            {
                gameModeChosen = true;
                Debug.Log($"Modo de juego seleccionado: {newMode}");
                TryStartMatchIfReady();

            };


           

            
        }
        base.OnNetworkSpawn();
    }
    public void readyButtonPressed()
    {
        readyButtonpressedServerRpc(OwnerClientId);
        readycanvas.SetActive(false);

    }


    [ServerRpc(RequireOwnership = false)]
    public void readyButtonpressedServerRpc(ulong clientId)
    {
        readyPlayersList.Add(clientId);
        Debug.Log($"[Server] Jugador {clientId} está listo.");
        TryStartMatchIfReady();

    }

    // ServerRpc público para que el Host lo invoque al elegir en UI
    [ServerRpc(RequireOwnership = false)]
    public void SelectGameModeServerRpc(GameMode mode)
    {
        SelectedGameMode.Value = mode;
        
    }


    public override void OnNetworkDespawn()
    {
        // Despawns de Netcode: limpia aquí
        humanList.Dispose();
        zombieList.Dispose();
        readyPlayersList.Dispose();
        playersNames.Dispose();

        base.OnNetworkDespawn();
    }



    private void OnDisable()
    {
        // Desuscribimos callbacks del servidor, si existían:
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        }

        // NO llamar a Dispose aquí; solo limpiar referencias si fuera necesario.
        // Comprobamos que cada lista exista, y la "ponemos a null" sin llamar Dispose().
        // El Dispose real se hará en OnDestroy() de abajo.

        if (humanList != null)
        {
            // (Opcional) desuscribir eventos de humanList si los tuvieras:
            // humanList.OnListChanged -= AlgunCallback;
            humanList = null;
        }

        if (zombieList != null)
        {
            // zombieList.OnListChanged -= AlgunCallback;
            zombieList = null;
        }

        if (readyPlayersList != null)
        {
            // readyPlayersList.OnListChanged -= AlgunCallback;
            readyPlayersList = null;
        }

        if (playersNames != null)
        {
            // playersNames.OnListChanged -= AlgunCallback;
            playersNames = null;
        }
    }

    public override void OnDestroy()
    {
        // Intentamos disponer de cada NetworkList, pero protegemos con try/catch
        if (humanList != null)
        {
            try
            {
                humanList.Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                // Si ya estaba dispuesto, simplemente ignoramos
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartGameVariables] Excepción al disponer humanList: {e}");
            }
            humanList = null;
        }

        if (zombieList != null)
        {
            try
            {
                zombieList.Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                // Ya estaba dispuesto
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartGameVariables] Excepción al disponer zombieList: {e}");
            }
            zombieList = null;
        }

        if (readyPlayersList != null)
        {
            try
            {
                readyPlayersList.Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                // Ya estaba dispuesto
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartGameVariables] Excepción al disponer readyPlayersList: {e}");
            }
            readyPlayersList = null;
        }

        if (playersNames != null)
        {
            try
            {
                playersNames.Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                // Ya estaba dispuesto
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartGameVariables] Excepción al disponer playersNames: {e}");
            }
            playersNames = null;
        }

        // Restauramos la referencia singleton si fuera necesario
        if (Instance == this)
        {
            Instance = null;
        }

        // Finalmente llamamos al OnDestroy de la base, para que Netcode haga su cleanup
        base.OnDestroy();
    }



}