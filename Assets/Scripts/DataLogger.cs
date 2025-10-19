using UnityEngine;
using System.IO; // Untuk file handling
using System.Text; // Untuk StringBuilder
using System; // Untuk DateTime

public class DataLogger : MonoBehaviour
{
    private StreamWriter writer; // Objek untuk menulis ke file
    private string filePath;
    private string currentGameID;

    // Dipanggil saat game dimulai
    public void InitializeCSV(string fileNamePrefix = "Llama_Log")
    {
        // Buat ID unik untuk game ini
        currentGameID = Guid.NewGuid().ToString("N").Substring(0, 8); // ID pendek 8 karakter

        // Tentukan path file (aman untuk build game)
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{fileNamePrefix}_{currentGameID}_{timeStamp}.csv";
        // Application.persistentDataPath adalah folder yang aman untuk menyimpan data
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            // Buat file baru (atau timpa jika sudah ada) dan tulis header
            writer = new StreamWriter(filePath, false, Encoding.UTF8); // false = overwrite
            // Header kolom CSV (sesuaikan sesuai kebutuhan)
            writer.WriteLine("GameID,Turn,Player,FEN_Before,StockfishOption1,StockfishOption2,StockfishOption3,LLM_RawResponse,LLM_ChosenMove,FinalExecutedMove,IsLLMRetryNeeded,IsLLMFailedMaxRetry,IsMoveDefault");
            writer.Flush(); // Pastikan header tertulis ke disk
            Debug.Log($"CSV Logger diinisialisasi. File disimpan di: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Gagal menginisialisasi CSV Logger: {e.Message}");
            writer = null; // Set null jika gagal
        }
    }

    // Fungsi utama untuk mencatat data per langkah/keputusan
    public void LogData(
        int turnNumber,
        string player, // "White" atau "Black"
        string fenBefore,
        string stockfishOption1 = "", // Default kosong
        string stockfishOption2 = "",
        string stockfishOption3 = "",
        string llmRawResponse = "",
        string llmChosenMove = "", // Pilihan LLM (bisa null/kosong)
        string finalExecutedMove = "", // Langkah yang benar-benar dimainkan
        bool isLLMRetryNeeded = false, // Apakah LLM gagal & perlu coba lagi?
        bool isLLMFailedMaxRetry = false, // Apakah LLM gagal max retry?
        bool isMoveDefault = false // Apakah langkah default yg dimainkan?
                                   // Tambahkan parameter lain jika perlu (misal: waktu berpikir)
    )
    {
        if (writer == null) return; // Jangan lakukan apa-apa jika file gagal dibuka

        try
        {
            // Escape koma dalam FEN (penting untuk CSV)
            string safeFen = $"\"{fenBefore.Replace("\"", "\"\"")}\""; // Bungkus FEN dengan ""

            // Buat baris data CSV
            StringBuilder sb = new StringBuilder();
            sb.Append(currentGameID).Append(",");
            sb.Append(turnNumber).Append(",");
            sb.Append(player).Append(",");
            sb.Append(safeFen).Append(",");
            sb.Append(stockfishOption1).Append(",");
            sb.Append(stockfishOption2).Append(",");
            sb.Append(stockfishOption3).Append(",");
            sb.Append($"\"{llmRawResponse.Replace("\"", "\"\"")}\"").Append(","); // Escape response LLM
            sb.Append(llmChosenMove ?? "").Append(","); // Ganti null jadi string kosong
            sb.Append(finalExecutedMove).Append(",");
            sb.Append(isLLMRetryNeeded).Append(",");
            sb.Append(isLLMFailedMaxRetry).Append(",");
            sb.Append(isMoveDefault); // Kolom terakhir tidak perlu koma

            // Tulis baris ke file
            writer.WriteLine(sb.ToString());
            writer.Flush(); // Langsung tulis ke disk (bagus untuk data penting)
        }
        catch (Exception e)
        {
            Debug.LogError($"Gagal menulis ke CSV: {e.Message}");
        }
    }

    // Dipanggil saat aplikasi/game ditutup
    void OnApplicationQuit()
    {
        if (writer != null)
        {
            Debug.Log("Menutup file CSV Logger.");
            writer.Close(); // Tutup file stream
            writer = null;
        }
    }

    // (Opsional) Fungsi untuk mendapatkan path file jika perlu
    public string GetFilePath()
    {
        return filePath;
    }
}