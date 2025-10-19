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
    private ChessGame logicBrain; //Menggunakan ChessGame, bukan ChessBoard
    public StockfishPlayer aiPlayer;
    public LLMPlayer llmPlayer;      //Ini Llama
    public float turnDelay = 1.0f; //Jeda antar giliran (detik)
    public Button stepButton;            // Slot untuk tombol UI
    public float manualStepTimeout = 15.0f; // Waktu timeout (detik)
    private bool isManualStepMode = false;  // Status mode saat ini
    private float manualStepTimer = 0f;    // Timer timeout
    private bool isAIMoving = false;
    public Text pgnLogText;           // Slot untuk LogText
    public ScrollRect pgnScrollRect;  // Slot untuk Scroll View
    public Text llmDecisionLogText;     // Slot untuk Text UI Keputusan LLM
    public ScrollRect llmDecisionScrollRect; // Slot untuk Scroll View Keputusan LLM
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
        // -----BARU: Bersihkan Log Keputusan LLM -----
        if (llmDecisionLogText != null) llmDecisionLogText.text = "";
        if (stepButton != null)
        {
            // Panggil fungsi ManualStep() saat tombol diklik
            stepButton.onClick.AddListener(ManualStep);
        }
        else
        {
            Debug.LogWarning("Tombol 'StepButton' belum di-assign di Inspector!");
        }
        // Mulai dalam mode otomatis
        isManualStepMode = true;
        manualStepTimer = manualStepTimeout;
        // ----------------------------------------
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

    // GANTI FUNGSI UPDATE LAMA DENGAN INI
    public void Update()
    {
        // Logika Restart Game (tidak berubah)
        if (gameOver == true && Input.GetMouseButtonDown(0))
        {
            gameOver = false;
            SceneManager.LoadScene("Game");
            return; // Hentikan update jika me-restart
        }

        // Jangan lakukan apa-apa jika game selesai atau AI sedang berpikir
        if (gameOver || isAIMoving)
        {
            // Opsional: Nonaktifkan tombol saat AI berpikir
            if (stepButton != null) stepButton.interactable = false;
            return;
        }
        else
        {
            // Aktifkan tombol jika AI tidak berpikir
            if (stepButton != null) stepButton.interactable = true;
        }


        // ----- Logika Mode Manual Step & Timeout -----
        if (isManualStepMode)
        {
            // Kurangi timer jika dalam mode manual
            manualStepTimer -= Time.deltaTime;
            if (manualStepTimer <= 0f)
            {
                // Timeout! Kembali ke mode otomatis
                isManualStepMode = false;
                manualStepTimer = 0f; // Reset timer
                Debug.Log("Timeout! Melanjutkan permainan otomatis.");
                // Jangan langsung trigger AI di sini, biarkan loop Update berikutnya
            }
            // Jika dalam mode manual dan timer masih > 0, JANGAN lakukan apa-apa.
            // Kita menunggu tombol ditekan via fungsi ManualStep().
            return; // Hentikan Update di sini jika menunggu input manual
        }
        // ----- Akhir Logika Mode Manual Step -----


        // ----- Logika Mode Otomatis (Hanya berjalan jika !isManualStepMode) -----
        // (Kode ini sama seperti sebelumnya, hanya saja sekarang ada jeda 'turnDelay')
        if (currentPlayer == "white")
        {
            isAIMoving = true;
            StartCoroutine(WaitAndExecuteMove(RequestLLMMove)); // Gunakan helper Coroutine
        }
        else if (currentPlayer == "black")
        {
            isAIMoving = true;
            StartCoroutine(WaitAndExecuteMove(RequestStockfishMove)); // Gunakan helper Coroutine
        }
    }
    // BARU: Helper untuk jeda di mode otomatis
    private IEnumerator WaitAndExecuteMove(System.Func<IEnumerator> moveRequestMethod)
    {
        // Hanya tunggu jika dalam mode otomatis
        if (!isManualStepMode && turnDelay > 0)
        {
            yield return new WaitForSeconds(turnDelay);
        }

        // Pastikan game tidak berakhir saat menunggu
        if (!gameOver)
        {
            StartCoroutine(moveRequestMethod());
        }
        else
        {
            isAIMoving = false; // Reset flag jika game berakhir saat menunggu
        }
    }
    // BARU: Fungsi yang dipanggil oleh tombol StepButton
    public void ManualStep()
    {
        // Jika AI sedang berpikir atau game over, jangan lakukan apa-apa
        if (isAIMoving || gameOver)
        {
            return;
        }

        // Set ke mode manual (atau perbarui timer jika sudah manual)
        isManualStepMode = true;
        manualStepTimer = manualStepTimeout; // Reset timer setiap kali tombol ditekan

        Debug.Log($"Tombol Step Ditekan. Giliran: {currentPlayer}. Memulai langkah...");

        // Langsung picu langkah AI untuk giliran saat ini
        isAIMoving = true; // Set flag sebelum memulai Coroutine
        if (currentPlayer == "white")
        {
            StartCoroutine(RequestLLMMove());
        }
        else // currentPlayer == "black"
        {
            StartCoroutine(RequestStockfishMove());
        }

        // Nonaktifkan tombol sementara AI berpikir
        if (stepButton != null) stepButton.interactable = false;
    }

    // GANTI COROUTINE LAMA DENGAN VERSI BARU INI
    private IEnumerator RequestLLMMove()
    {
        // Beri jeda
        yield return new WaitForSeconds(turnDelay);

        // 1. Dapatkan FEN
        string currentFen = logicBrain.GetFen();

        // 2. Minta 3 LANGKAH TERBAIK dari Stockfish
        Debug.Log("[LLM Step 1] Meminta 3 saran dari Stockfish...");
        Task<List<string>> topMovesTask = aiPlayer.RequestTopMoves(currentFen, 3);

        // 3. Tunggu sampai Stockfish selesai
        Debug.Log("[LLM Step 1] Menunggu Task Stockfish selesai...");
        yield return new WaitUntil(() => topMovesTask.IsCompleted);
        Debug.Log("[LLM Step 1] Task Stockfish SELESAI.");

        // 4. Ambil hasilnya (berupa List<string>)
        List<string> topMoves = topMovesTask.Result;

        // 5. Validasi hasil dari Stockfish
        if (topMoves == null || topMoves.Count == 0)
        {
            Debug.LogError("Stockfish gagal memberikan saran langkah!");
            isAIMoving = false;
            yield break;
        }

        // ----- PERBAIKAN LOGIKA LOGGING -----
        // 6. Siapkan string log AWAL (FEN + Opsi + Meminta)
        string decisionLog = $"Giliran {moveCounter} (Putih - LLM)\n";
        decisionLog += $"FEN: {currentFen}\n";
        decisionLog += "Stockfish menyarankan:\n";
        for (int i = 0; i < topMoves.Count; i++)
        {
            decisionLog += $"{i + 1}. {topMoves[i]}\n";
        }
        decisionLog += "\nMeminta LLM untuk memilih...\n"; // Tambahkan pesan menunggu

        // 7. Update UI Log SEBELUM memanggil LLM (Tampilkan FEN, Opsi, Menunggu)
        if (llmDecisionLogText != null)
        {
            llmDecisionLogText.text += decisionLog; // Tambahkan log awal
            TrimUIText(llmDecisionLogText);
            StartCoroutine(ForceScrollDown(llmDecisionScrollRect));
        }

        // 8. Panggil LLM untuk memilih dari 3 opsi
        Debug.Log("[LLM Step 2] Meminta LLM memilih...");
        Task<string> choiceTask = llmPlayer.ChooseBestMove(currentFen, topMoves);

        // 9. Tunggu LLM selesai berpikir
        Debug.Log("[LLM Step 2] Menunggu Task LLM selesai...");
        yield return new WaitUntil(() => choiceTask.IsCompleted);
        Debug.Log("[LLM Step 2] Task LLM SELESAI.");

        // 10. Ambil pilihan LLM (bisa null jika LLM gagal)
        string chosenMove = choiceTask.Result;

        // 11. Siapkan string log AKHIR (Hasil Pilihan LLM)
        string choiceResultLog = "";
        if (chosenMove != null)
        {
            choiceResultLog = $"LLM Memilih: {chosenMove}\n---\n";
        }
        else
        {
            choiceResultLog = $"LLM GAGAL memilih dari opsi!\n---\n";
        }

        // 12. Update UI Log LAGI (Tambahkan hasil pilihan ke log yang sudah ada)
        if (llmDecisionLogText != null)
        {
            llmDecisionLogText.text += choiceResultLog; // Tambahkan hasil pilihan
            TrimUIText(llmDecisionLogText);
            StartCoroutine(ForceScrollDown(llmDecisionScrollRect));
        }
        // ----- AKHIR PERBAIKAN LOGGING -----


        // 13. Coba lakukan langkah yang dipilih
        if (chosenMove != null && TryMakeMove(chosenMove))
        {
            // Sukses
            isAIMoving = false;
        }
        else
        {
            // Gagal / Retry
            if (chosenMove == null)
            {
                UnityEngine.Debug.LogWarning($"LLM GAGAL memilih langkah yang valid dari opsi. Mencoba lagi...");
            }
            else
            {
                // Seharusnya tidak terjadi jika LLM memilih dari opsi Stockfish
                UnityEngine.Debug.LogError($"Langkah '{chosenMove}' yang dipilih LLM GAGAL dieksekusi oleh TryMakeMove! FEN: {currentFen}");
            }
            UnityEngine.Debug.Log("[LLM] Coroutine SELESAI. Menyetel isAIMoving = false.");
            isAIMoving = false; // Izinkan retry loop
        }
    }
    private IEnumerator RequestStockfishMove()
    {
        // 1. Dapatkan FEN (status papan saat ini) dari otak logika
        string currentFen = logicBrain.GetFen();

        // 2. Minta AI untuk berpikir (ini berjalan di thread terpisah)
        Debug.Log("[Stockfish] Meminta langkah...");
        Task<string> moveTask = aiPlayer.RequestBestMove(currentFen);

        // 3. Tunggu di sini tanpa membekukan game sampai task-nya selesai
        Debug.Log("[Stockfish] Menunggu Task selesai...");
        yield return new WaitUntil(() => moveTask.IsCompleted);
        Debug.Log("[Stockfish] Task SELESAI.");

        // 4. Ambil hasilnya
        string bestMove = moveTask.Result;

        // 5. Lakukan langkah di game
        if (bestMove != null)
        {
            // TryMakeMove sudah menangani logika visual dan giliran
            TryMakeMove(bestMove);
        }

        // 6. Setel ulang flag agar AI bisa bergerak lagi nanti
        UnityEngine.Debug.Log("[Stockfish] Coroutine SELESAI. Menyetel isAIMoving = false.");
        isAIMoving = false;
    }

    public void Winner(string result) // Ubah parameter jadi 'result'
    {
        gameOver = true;

        // Dapatkan referensi ke semua komponen Text
        Text winnerTextComponent = GameObject.FindGameObjectWithTag("WinnerText")?.GetComponent<Text>();
        Text drawTextComponent = GameObject.FindGameObjectWithTag("DrawText")?.GetComponent<Text>();
        Text restartTextComponent = GameObject.FindGameObjectWithTag("RestartText")?.GetComponent<Text>();

        // Sembunyikan semua teks hasil dulu
        if (winnerTextComponent != null) winnerTextComponent.enabled = false;
        if (drawTextComponent != null) drawTextComponent.enabled = false;

        // Tampilkan teks yang sesuai
        if (result.ToLower() == "draw")
        {
            if (drawTextComponent != null)
            {
                drawTextComponent.enabled = true;
                // drawTextComponent.text = "DRAW"; // Jika perlu di-set
            }
            else
            {
                Debug.LogError("GameObject dengan Tag 'DrawText' tidak ditemukan!");
            }
        }
        else // Jika bukan Draw, berarti ada pemenang
        {
            if (winnerTextComponent != null)
            {
                winnerTextComponent.enabled = true;
                winnerTextComponent.text = result.ToUpper() + " IS THE WINNER"; // result = "White" atau "Black"
            }
            else
            {
                Debug.LogError("GameObject dengan Tag 'WinnerText' tidak ditemukan!");
            }
        }

        // Selalu tampilkan teks restart
        if (restartTextComponent != null)
        {
            restartTextComponent.enabled = true;
        }
        else
        {
            Debug.LogError("GameObject dengan Tag 'RestartText' tidak ditemukan!");
        }
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
    // GANTI SELURUH FUNGSI INI DENGAN VERSI FINAL
    // GANTI SELURUH FUNGSI INI DENGAN VERSI FINAL
    public bool TryMakeMove(string algebraicMove)
    {
        // --- Bagian Awal (Parsing Input & Safety Check) ---
        string fromAlg = "" + algebraicMove[0] + algebraicMove[1];
        string toAlg = "" + algebraicMove[2] + algebraicMove[3];
        Player playerToMove = logicBrain.WhoseTurn;

        Vector2Int fromCoords = GetCoordsFromAlgebraic(fromAlg);
        Vector2Int toCoords = GetCoordsFromAlgebraic(toAlg);

        GameObject pieceToMove = GetPosition(fromCoords.x, fromCoords.y);
        if (pieceToMove == null)
        {
            UnityEngine.Debug.LogError($"ERROR: TryMakeMove gagal karena pieceToMove di {fromAlg} adalah NULL. FEN: {logicBrain.GetFen()}");
            return false;
        }

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

        // --- Validasi Langkah ---
        if (logicBrain.IsValidMove(move))
        {
            // Deteksi Rokade (sebelum MakeMove)
            bool isCastleKingside = false;
            bool isCastleQueenside = false;
            if (pieceToMove.name.Contains("king"))
            {
                int deltaX = toCoords.x - fromCoords.x;
                if (deltaX == 2) isCastleKingside = true;
                else if (deltaX == -2) isCastleQueenside = true;
            }

            // 1. Eksekusi langkah di otak logika
            logicBrain.MakeMove(move, true);

            // ----- BARU: CEK GAME OVER DILAKUKAN DI SINI -----
            Player nextPlayer = logicBrain.WhoseTurn; // Pemain yang *akan* bergerak
            bool isGameOverNow = false;
            if (logicBrain.IsCheckmated(nextPlayer))
            {
                isGameOverNow = true;
                Winner(playerToMove == Player.White ? "White" : "Black"); // Pemenangnya adalah yang baru saja bergerak
            }
            else if (logicBrain.IsStalemated(nextPlayer) ||
                     logicBrain.IsInsufficientMaterial())
            {
                isGameOverNow = true;
                Winner("Draw");
            }
            // ---------------------------------------------

            // 2. Eksekusi langkah visual UTAMA
            GameObject pieceToCapture = GetPosition(toCoords.x, toCoords.y);
            if (pieceToCapture != null) Destroy(pieceToCapture); // Hapus bidak yang dimakan (lebih ringkas)

            SetPositionEmpty(fromCoords.x, fromCoords.y);
            pieceToMove.GetComponent<Chessman>().SetXBoard(toCoords.x);
            pieceToMove.GetComponent<Chessman>().SetYBoard(toCoords.y);
            pieceToMove.GetComponent<Chessman>().SetCoords();
            SetPosition(pieceToMove);

            // Logika Visual Rokade (tidak berubah)
            if (isCastleKingside) { /* ... kode pindah benteng ... */ }
            else if (isCastleQueenside) { /* ... kode pindah benteng ... */ }
            // (Pastikan kode pemindahan benteng Anda sudah benar di sini)
            // Contoh singkat:
            if (isCastleKingside) MoveRookForCastle(playerToMove, true);
            else if (isCastleQueenside) MoveRookForCastle(playerToMove, false);


            // Log PGN (Hanya jika game TIDAK berakhir di langkah ini)
            if (!isGameOverNow)
            {
                Player playerWhoMoved = playerToMove;
                if (playerWhoMoved == Player.White)
                {
                    pgnHistory += $"{moveCounter}. {algebraicMove} ";
                }
                else
                {
                    pgnHistory += $"{algebraicMove}\n";
                    moveCounter++;
                }
                if (pgnLogText != null)
                {
                    pgnLogText.text = pgnHistory;
                    TrimUIText(pgnLogText);
                }
                StartCoroutine(ForceScrollDown(pgnScrollRect));

                // Ganti giliran (Hanya jika game tidak berakhir)
                NextTurn();
            }

            // Jika game berakhir, kita tidak ganti giliran dan tidak log PGN lagi
            // Kita langsung return true karena langkahnya valid
            return true;
        }

        UnityEngine.Debug.LogWarning($"Langkah tidak valid DITOLAK oleh logicBrain: {algebraicMove} | FEN: {logicBrain.GetFen()}");
        return false; // Langkah tidak valid
    }

    // BARU (Opsional): Helper untuk memindahkan benteng saat rokade agar lebih rapi
    private void MoveRookForCastle(Player player, bool kingside)
    {
        Vector2Int rookFromCoords, rookToCoords;
        if (kingside)
        {
            rookFromCoords = (player == Player.White) ? new Vector2Int(7, 0) : new Vector2Int(7, 7); // H1/H8
            rookToCoords = (player == Player.White) ? new Vector2Int(5, 0) : new Vector2Int(5, 7); // F1/F8
        }
        else
        { // Queenside
            rookFromCoords = (player == Player.White) ? new Vector2Int(0, 0) : new Vector2Int(0, 7); // A1/A8
            rookToCoords = (player == Player.White) ? new Vector2Int(3, 0) : new Vector2Int(3, 7); // D1/D8
        }

        GameObject rook = GetPosition(rookFromCoords.x, rookFromCoords.y);
        if (rook != null)
        {
            SetPositionEmpty(rookFromCoords.x, rookFromCoords.y);
            rook.GetComponent<Chessman>().SetXBoard(rookToCoords.x);
            rook.GetComponent<Chessman>().SetYBoard(rookToCoords.y);
            rook.GetComponent<Chessman>().SetCoords();
            SetPosition(rook);
        }
        else
        {
            UnityEngine.Debug.LogError($"Rokade {(kingside ? "Kingside" : "Queenside")}: Benteng tidak ditemukan di {GetAlgebraicFromCoords(rookFromCoords.x, rookFromCoords.y)}!");
        }
    }
    // BARU: Coroutine untuk memaksa scrollbar ke paling bawah
    private IEnumerator ForceScrollDown(ScrollRect scrollRectToScroll) // <-- BARU: Parameter
    {
        // Tunggu hingga akhir frame
        yield return new WaitForEndOfFrame();

        // Paksa scrollbar ke bawah
        if (scrollRectToScroll != null)
        {
            scrollRectToScroll.verticalNormalizedPosition = 0f;
        }
    }
    private void TrimUIText(Text uiText, int maxChars = 20000, int keepChars = 15000)
    {
        if (uiText != null && uiText.text.Length > maxChars)
        {
            uiText.text = uiText.text.Substring(uiText.text.Length - keepChars);
        }
    }
}