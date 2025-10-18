using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // <-- Penting untuk Process
using System.IO;          // <-- Penting untuk Stream
using System.Threading.Tasks; // <-- Penting untuk Async

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
}