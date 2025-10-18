using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChessDotNet; // <-- BARU: Menggunakan library yang benar
using ChessDotNet.Pieces; // <-- BARU: Dibutuhkan untuk Catur
using System.Threading.Tasks;

public class Game : MonoBehaviour
{
    //Reference from Unity IDE
    public GameObject chesspiece;

    //Matrices needed, positions of each of the GameObjects
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    // ----- OTAK LOGIKA BARU -----
    private ChessGame logicBrain; // <-- BARU: Menggunakan ChessGame, bukan ChessBoard
    public StockfishPlayer aiPlayer;
    private bool isAIMoving = false;
    public Text pgnLogText;           // Slot untuk LogText
    public ScrollRect pgnScrollRect;  // Slot untuk Scroll View
    private int moveCounter = 1;
    private string pgnHistory = "";
    // ----------------------------

    //current turn
    private string currentPlayer = "white";

    //Game Ending
    private bool gameOver = false;

    //Unity calls this right when the game starts
    public void Start()
    {
        playerWhite = new GameObject[] { Create("white_rook", 0, 0), Create("white_knight", 1, 0),
            Create("white_bishop", 2, 0), Create("white_queen", 3, 0), Create("white_king", 4, 0),
            Create("white_bishop", 5, 0), Create("white_knight", 6, 0), Create("white_rook", 7, 0),
            Create("white_pawn", 0, 1), Create("white_pawn", 1, 1), Create("white_pawn", 2, 1),
            Create("white_pawn", 3, 1), Create("white_pawn", 4, 1), Create("white_pawn", 5, 1),
            Create("white_pawn", 6, 1), Create("white_pawn", 7, 1) };
        playerBlack = new GameObject[] { Create("black_rook", 0, 7), Create("black_knight",1,7),
            Create("black_bishop",2,7), Create("black_queen",3,7), Create("black_king",4,7),
            Create("black_bishop",5,7), Create("black_knight",6,7), Create("black_rook",7,7),
            Create("black_pawn", 0, 6), Create("black_pawn", 1, 6), Create("black_pawn", 2, 6),
            Create("black_pawn", 3, 6), Create("black_pawn", 4, 6), Create("black_pawn", 5, 6),
            Create("black_pawn", 6, 6), Create("black_pawn", 7, 6) };

        //Set all piece positions on the positions board
        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }

        // ----- BARU: INISIALISASI OTAK LOGIKA -----
        logicBrain = new ChessGame(); // <-- BARU: Inisialisasi ChessGame
        // ------------------------------------------
        if (pgnLogText != null) pgnLogText.text = "";
    }

    public GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        Chessman cm = obj.GetComponent<Chessman>();
        obj.name = name;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        return obj;
    }

    public void SetPosition(GameObject obj)
    {
        Chessman cm = obj.GetComponent<Chessman>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
        {
            currentPlayer = "black";
        }
        else
        {
            currentPlayer = "white";
        }
    }

    public void Update()
    {
        if (gameOver == true && Input.GetMouseButtonDown(0))
        {
            gameOver = false;
            SceneManager.LoadScene("Game");
        }

        // ----- BARU: Logika Pemicu AI -----
        // Kita akan set AI sebagai "black"
        if (currentPlayer == "black" && !gameOver && !isAIMoving)
        {
            isAIMoving = true;
            StartCoroutine(RequestAIMove());
        }
    }

    private IEnumerator RequestAIMove()
    {
        // 1. Dapatkan FEN (status papan saat ini) dari otak logika
        string currentFen = logicBrain.GetFen();

        // 2. Minta AI untuk berpikir (ini berjalan di thread terpisah)
        Task<string> moveTask = aiPlayer.RequestBestMove(currentFen);

        // 3. Tunggu di sini tanpa membekukan game sampai task-nya selesai
        yield return new WaitUntil(() => moveTask.IsCompleted);

        // 4. Ambil hasilnya
        string bestMove = moveTask.Result;

        // 5. Lakukan langkah di game
        if (bestMove != null)
        {
            // TryMakeMove sudah menangani logika visual dan giliran
            TryMakeMove(bestMove);
        }

        // 6. Setel ulang flag agar AI bisa bergerak lagi nanti
        isAIMoving = false;
    }

    public void Winner(string playerWinner)
    {
        gameOver = true;
        GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().enabled = true;
        GameObject.FindGameObjectWithTag("WinnerText").GetComponent<Text>().text = playerWinner + " is the winner";
        GameObject.FindGameObjectWithTag("RestartText").GetComponent<Text>().enabled = true;
    }

    // --------------------------------------------------
    // ----- FUNGSI BARU UNTUK LOGIKA CATUR & AI -----
    // --------------------------------------------------

    // Fungsi ini tidak berubah
    public string GetAlgebraicFromCoords(int x, int y)
    {
        char file = (char)('a' + x);
        char rank = (char)('1' + y);
        return "" + file + rank;
    }

    // Fungsi ini tidak berubah
    public Vector2Int GetCoordsFromAlgebraic(string alg)
    {
        int x = char.ToLower(alg[0]) - 'a'; // <-- PERBAIKAN: Paksa huruf pertama jadi lowercase
        int y = alg[1] - '1';
        return new Vector2Int(x, y);
    }

    // BARU: Memberi akses skrip lain ke otak catur
    public ChessGame GetLogicBrain()
    {
        return logicBrain;
    }

    // BARU: Fungsi terpusat untuk menghapus MovePlate
    public void DestroyAllMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }

    // BARU: FUNGSI MASTER "SANG WASIT" (Ditulis ulang untuk ChessDotNet)
    // GANTI FUNGSI LAMA ANDA DENGAN INI
    public bool TryMakeMove(string algebraicMove)
    {
        string fromAlg = "" + algebraicMove[0] + algebraicMove[1];
        string toAlg = "" + algebraicMove[2] + algebraicMove[3];
        Player playerToMove = logicBrain.WhoseTurn;

        Vector2Int fromCoords = GetCoordsFromAlgebraic(fromAlg);
        Vector2Int toCoords = GetCoordsFromAlgebraic(toAlg);

        // ----- PERBAIKAN PENGAMANAN -----
        GameObject pieceToMove = GetPosition(fromCoords.x, fromCoords.y);
        if (pieceToMove == null)
        {
            UnityEngine.Debug.LogError($"ERROR: TryMakeMove gagal karena pieceToMove di {fromAlg} adalah NULL.");
            return false;
        }
        // ---------------------------------

        Position fromPos = new Position(fromAlg);
        Position toPos = new Position(toAlg);
        Move move;

        // Penanganan Promosi
        if (pieceToMove.name.Contains("pawn") && (toCoords.y == 7 || toCoords.y == 0))
        {
            move = new Move(fromPos, toPos, playerToMove, 'Q');
        }
        else
        {
            move = new Move(fromPos, toPos, playerToMove);
        }

        // Cek ke otak catur apakah langkah ini legal
        if (logicBrain.IsValidMove(move))
        {
            // 1. Eksekusi langkah di otak logika
            logicBrain.MakeMove(move, true);

            // 2. Eksekusi langkah di papan visual
            GameObject pieceToCapture = GetPosition(toCoords.x, toCoords.y);
            if (pieceToCapture != null)
            {
                if (pieceToCapture.name == "white_king") Winner("black");
                if (pieceToCapture.name == "black_king") Winner("white");
                Destroy(pieceToCapture);
            }

            // Pindahkan bidak
            SetPositionEmpty(fromCoords.x, fromCoords.y);
            pieceToMove.GetComponent<Chessman>().SetXBoard(toCoords.x);
            pieceToMove.GetComponent<Chessman>().SetYBoard(toCoords.y);

            // PERBAIKAN: Panggil SetCoords() SEBELUM SetPosition()
            // Ini memastikan internal (x,y) bidak sudah benar SEBELUM kita menaruhnya di array 'positions'
            pieceToMove.GetComponent<Chessman>().SetCoords();
            SetPosition(pieceToMove);

            // Ganti giliran
            NextTurn();

            // Cek Game Over
            Player nextPlayer = logicBrain.WhoseTurn;
            if (logicBrain.IsCheckmated(nextPlayer))
            {
                gameOver = true;
                Winner(nextPlayer == Player.White ? "black" : "white");
            }
            else if (logicBrain.IsStalemated(nextPlayer) ||
                     logicBrain.IsInsufficientMaterial())
            {
                gameOver = true;
                Winner("Draw");
            }

            Player playerWhoMoved = playerToMove;

            if (playerWhoMoved == Player.White)
            {
                // Ini langkah Putih, tambahkan nomor langkah
                // Format: "1. e2e4 " (dengan spasi)
                pgnHistory += $"{moveCounter}. {algebraicMove} ";
            }
            else
            {
                // Ini langkah Hitam, tambahkan baris baru
                // Format: "e7e5\n"
                pgnHistory += $"{algebraicMove}\n";
                moveCounter++; // Naikkan nomor HANYA setelah Hitam bergerak
            }

            // Update UI Text
            if (pgnLogText != null)
            {
                pgnLogText.text = pgnHistory;
            }

            // Paksa scroll ke bawah
            StartCoroutine(ForceScrollDown());

            // ----------------------------------------

            return true; // Langkah berhasil
        }


        UnityEngine.Debug.LogWarning($"Langkah tidak valid DITOLAK oleh logicBrain: {algebraicMove}");
        return false; // Langkah tidak valid
    }
    // BARU: Coroutine untuk memaksa scrollbar ke paling bawah
    private IEnumerator ForceScrollDown()
    {
        // Tunggu hingga akhir frame (saat UI selesai di-render)
        yield return new WaitForEndOfFrame();

        // Paksa nilai scrollbar ke 0 (paling bawah)
        if (pgnScrollRect != null)
        {
            pgnScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}