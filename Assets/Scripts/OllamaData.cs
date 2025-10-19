using System;
using System.Collections.Generic; // Dibutuhkan untuk List

// ----- INI DATA BARU UNTUK /api/chat -----
[Serializable]
public class OllamaChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class OllamaChatRequest
{
    public string model;
    public List<OllamaChatMessage> messages;
    public bool stream = false;
}

// ----- INI DATA BARU DARI /api/chat -----
[Serializable]
public class OllamaChatResponse
{
    public string model;
    public OllamaChatMessage message;
    // (Abaikan data lainnya)
}