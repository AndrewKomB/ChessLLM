using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    //Some functions will need reference to the controller
    public GameObject controller;

    //The Chesspiece that was tapped to create this MovePlate
    GameObject reference = null;

    //Location on the board
    int matrixX;
    int matrixY;

    //false: movement, true: attacking
    public bool attack = false;

    public void Start()
    {
        if (attack)
        {
            //Set to red
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }

    // Logika ini sudah benar
    public void OnMouseUp()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");
        Game gameScript = controller.GetComponent<Game>();

        // Dapatkan koordinat 'from' dari bidak yang di-klik
        Chessman cm = reference.GetComponent<Chessman>();
        string fromAlg = gameScript.GetAlgebraicFromCoords(cm.GetXBoard(), cm.GetYBoard());

        // Dapatkan koordinat 'to' dari posisi plat ini
        string toAlg = gameScript.GetAlgebraicFromCoords(matrixX, matrixY);

        // Buat string langkah lengkap, misal: "e2e4"
        string move = fromAlg + toAlg;

        // Minta "Wasit" (Game.cs) untuk mencoba langkah ini
        gameScript.TryMakeMove(move);

        // Hancurkan semua move plate, baik langkahnya berhasil atau tidak
        gameScript.DestroyAllMovePlates();
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }
}