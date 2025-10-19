using System.Collections.Generic; // Dibutuhkan untuk List
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;

public class LLMPlayer : MonoBehaviour
{
    // Pastikan nama model ini SAMA PERSIS dengan yang ada di Ollama
    public string modelName = "mistral:latest";

    // ----- PERBAIKAN: Menggunakan endpoint /api/chat -----
    private string ollamaUrl = "http://localhost:11434/api/chat";

    // Fungsi utama yang akan dipanggil oleh Game.cs
    public async Task<string> ChooseBestMove(string fen, List<string> options) // <-- NAMA & PARAMETER BARU
    {
        string prompt = BuildChoicePrompt(fen, options);

        // 1. Membuat Request JSON (Format /api/chat BARU)
        OllamaChatRequest requestData = new OllamaChatRequest
        {
            model = modelName,
            messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { role = "user", content = prompt }
            }
        };
        string jsonRequest = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);

        // 2. Menyiapkan UnityWebRequest (koneksi ke server Ollama)
        UnityWebRequest webRequest = new UnityWebRequest(ollamaUrl, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        // 3. Mengirim Request dan Menunggu Jawaban
        //    (Kita tidak lagi menggunakan Awaiter helper yang buggy)
        try
        {
            await webRequest.SendWebRequest();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LLM Request Error: {e.Message}");
            return null; // Kembalikan null jika koneksi error
        }

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"LLM Error: {webRequest.error} | {webRequest.downloadHandler.text}");
            return null;
        }

        // 4. Mem-parsing dan Membersihkan Jawaban (Format BARU)
        string jsonResponse = webRequest.downloadHandler.text;
        OllamaChatResponse ollamaResponse = JsonUtility.FromJson<OllamaChatResponse>(jsonResponse);

        // Respons sekarang ada di dalam objek 'message'
        string rawResponse = ollamaResponse.message.content;
        string cleanedMove = CleanResponse(rawResponse, options);

        Debug.Log($"LLM Raw: '{rawResponse}' | Cleaned: '{cleanedMove}'");

        return cleanedMove;
    }

    // Prompt
    private string BuildChoicePrompt(string fen, List<string> options)
    {
        string optionsString = "";
        for (int i = 0; i < options.Count; i++)
        {
            optionsString += $"{i + 1}. {options[i]}\n"; // Format: 1. e2e4 \n 2. g1f3 \n ...
        }

        return $"You are a world-class chess grandmaster evaluating potential moves. Given the following FEN position and three candidate moves (in long algebraic notation), your task is to choose the single best move from the options provided.\n\n" +
               $"FEN: {fen}\n\n" +
               $"Candidate Moves:\n" +
               $"{optionsString}\n" + // Masukkan 3 opsi di sini
               $"Which move do you choose? Respond ONLY with the chosen move notation (e.g., e2e4). Do not include the number, explanation, or any other text.\n\n" +
               $"Your choice:";
    }

    // Fungsi pembersih ini tidak berubah
    // GANTI FUNGSI LAMA DENGAN INI (Lebih Ketat)
    private string CleanResponse(string rawResponse, List<string> validOptions) // <-- Tambah parameter opsi
    {
        // 1. Coba cari kecocokan persis (case-insensitive) dengan salah satu opsi
        foreach (string option in validOptions)
        {
            // Gunakan Regex.IsMatch untuk pencocokan yang lebih fleksibel (mengabaikan spasi/newline di awal/akhir)
            // dan case-insensitive. Pola @"\b" memastikan kita mencocokkan kata utuh.
            if (Regex.IsMatch(rawResponse.Trim(), @"\b" + Regex.Escape(option) + @"\b", RegexOptions.IgnoreCase))
            {
                return option; // Kembalikan opsi asli (dengan case yang benar)
            }
        }

        // 2. Jika tidak ada kecocokan persis, coba Regex lama sebagai fallback (mungkin LLM memberi format benar tapi bukan salah satu opsi?)
        Match match = Regex.Match(rawResponse, @"[a-h][1-8][a-h][1-8][qrbn]?");
        if (match.Success)
        {
            string potentialMove = match.Value;
            // Periksa lagi apakah hasil regex ini ada di opsi valid
            if (validOptions.Contains(potentialMove, StringComparer.OrdinalIgnoreCase))
            {
                // Cari case asli dari opsi
                return validOptions.First(opt => opt.Equals(potentialMove, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Jika tidak ada yang cocok, kembalikan null untuk memicu retry
        Debug.LogWarning($"LLM Response '{rawResponse.Trim()}' tidak cocok dengan opsi: {string.Join(", ", validOptions)}");
        return null;
    }
}