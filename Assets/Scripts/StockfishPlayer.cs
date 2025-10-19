using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // <-- Penting untuk Process
using System.IO;          // <-- Penting untuk Stream
using System.Threading.Tasks; // <-- Penting untuk Async
using System.Linq; // <-- BARU: Dibutuhkan untuk LINQ

public class StockfishPlayer : MonoBehaviour
{
    private Process stockfishProcess;
    private StreamWriter processInput;
    private StreamReader processOutput;

    private Task<string> currentMoveTask;
    
    void Start()
    {
        // Tentukan path ke stockfish.exe di folder StreamingAssets
        string stockfishPath = Path.Combine(Application.streamingAssetsPath, "stockfish.exe");

        // Konfigurasi proses untuk dijalankan
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = stockfishPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true // Sembunyikan jendela konsol hitam
        };

        // Mulai proses
        try
        {
            stockfishProcess = Process.Start(startInfo);
            if (stockfishProcess == null)
            {
                UnityEngine.Debug.LogError("Gagal memulai proses Stockfish!");
                return;
            }

            processInput = stockfishProcess.StandardInput;
            processOutput = stockfishProcess.StandardOutput;

            // Beri tahu Stockfish untuk menggunakan protokol UCI
            InitializeUCI();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error memulai Stockfish: {e.Message}");
        }
    }

    private async void InitializeUCI()
    {
        await processInput.WriteLineAsync("uci");
        string output;
        while ((output = await processOutput.ReadLineAsync()) != null)
        {
            if (output == "uciok")
            {
                await processInput.WriteLineAsync("isready");
            }
            if (output == "readyok")
            {
                UnityEngine.Debug.Log("Stockfish Siap (UCI Ready)."); // Log ke Console
                break;
            }
        }
    }


    // Ini adalah fungsi publik yang akan dipanggil oleh Game.cs
    // Ini berjalan secara asynchronous (di background) agar game tidak freeze
    public async Task<string> RequestBestMove(string fen)
    {
        if (processInput == null || processOutput == null)
        {
            UnityEngine.Debug.LogError("Stockfish process tidak terinisialisasi.");
            return null;
        }

        await processInput.WriteLineAsync($"position fen {fen}");
        await processInput.WriteLineAsync("go movetime 1000");

        string output;
        while ((output = await processOutput.ReadLineAsync()) != null)
        {
            // Kita masih bisa log "pikiran" ke Console jika mau, tapi tidak ke UI
            // if (output.StartsWith("info")) UnityEngine.Debug.Log(output);

            if (output.StartsWith("bestmove"))
            {
                string bestMove = output.Split(' ')[1];
                UnityEngine.Debug.Log($"Stockfish memilih: {bestMove}"); // Log ke Console
                return bestMove;
            }
        }
        return null;
    }

    // Pastikan proses Stockfish ditutup saat game berhenti
    void OnApplicationQuit()
    {
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            processInput.WriteLine("quit");
            stockfishProcess.WaitForExit(2000); // Tunggu 2 detik
            stockfishProcess.Kill();
            stockfishProcess.Close();
        }
    }
    public async Task<List<string>> RequestTopMoves(string fen, int numberOfMoves = 3)
    {
        if (processInput == null || processOutput == null)
        {
            UnityEngine.Debug.LogError("Stockfish process tidak terinisialisasi.");
            return null;
        }

        List<string> topMoves = new List<string>();
        List<int> scores = new List<int>(); // Untuk menyimpan skor (opsional, tapi bagus untuk debug)

        // 1. Set Stockfish untuk mencari N langkah terbaik (MultiPV)
        await processInput.WriteLineAsync($"setoption name MultiPV value {numberOfMoves}");

        // 2. Atur posisi papan
        await processInput.WriteLineAsync($"position fen {fen}");

        // 3. Minta Stockfish untuk berpikir (misal: 1 detik)
        await processInput.WriteLineAsync("go movetime 1000");

        // 4. Baca output dan parse baris "info" untuk "pv"
        string output;
        while ((output = await processOutput.ReadLineAsync()) != null)
        {
            // Kirim output mentah ke log UI (jika kita mau nanti)
            // logQueue.Enqueue(output); // (Di-comment dulu)

            if (output.StartsWith("info") && output.Contains(" pv "))
            {
                // Contoh: info depth 15 score cp 13 multipv 1 nodes ... pv e2e4 e7e5 g1f3
                string[] parts = output.Split(new string[] { " pv " }, System.StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    string[] moves = parts[1].Split(' ');
                    if (moves.Length > 0 && !string.IsNullOrEmpty(moves[0]))
                    {
                        string move = moves[0];

                        // Ekstrak skor (centipawn)
                        int score = 0;
                        int scoreIndex = output.IndexOf(" score cp ");
                        if (scoreIndex > 0)
                        {
                            string scoreStr = output.Substring(scoreIndex + 10).Split(' ')[0];
                            int.TryParse(scoreStr, out score);
                        }

                        // Ekstrak multipv number
                        int multipv = 0;
                        int multipvIndex = output.IndexOf(" multipv ");
                        if (multipvIndex > 0)
                        {
                            string multipvStr = output.Substring(multipvIndex + 9).Split(' ')[0];
                            int.TryParse(multipvStr, out multipv);
                        }

                        // Pastikan kita menyimpan dalam urutan MultiPV (1, 2, 3)
                        // dan hanya menyimpan langkah unik dari iterasi depth terakhir
                        if (multipv > 0 && multipv <= numberOfMoves)
                        {
                            // Gunakan index multipv-1 (karena list 0-based)
                            int index = multipv - 1;
                            // Pastikan list cukup besar
                            while (topMoves.Count <= index)
                            {
                                topMoves.Add(string.Empty);
                                scores.Add(int.MinValue);
                            }
                            // Simpan langkah dan skor (akan dioverwrite oleh depth yg lebih dalam)
                            topMoves[index] = move;
                            scores[index] = score;
                        }
                    }
                }
            }

            // Hentikan membaca jika Stockfish selesai berpikir
            if (output.StartsWith("bestmove"))
            {
                break; // Keluar dari while loop
            }
        }

        // Bersihkan list dari entri kosong jika ada
        topMoves.RemoveAll(string.IsNullOrEmpty);

        // Reset MultiPV ke 1 untuk pemanggilan berikutnya (penting!)
        await processInput.WriteLineAsync("setoption name MultiPV value 1");

        UnityEngine.Debug.Log($"Stockfish Top {topMoves.Count} Moves: {string.Join(", ", topMoves)}");
        return topMoves;
    }
}