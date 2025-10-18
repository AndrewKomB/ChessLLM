using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // <-- Diperlukan untuk .Where()
using ChessDotNet; // <-- BARU: Menggunakan library yang benar

public class Chessman : MonoBehaviour
{
    //References to objects in our Unity Scene
    public GameObject controller;
    public GameObject movePlate;

    //Position for this Chesspiece on the Board
    private int xBoard = -1;
    private int yBoard = -1;

    //Variable for keeping track of the player it belongs to "black" or "white"
    private string player;

    //References to all the possible Sprites that this Chesspiece could be
    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    public void Activate()
    {
        //Get the game controller
        controller = GameObject.FindGameObjectWithTag("GameController");

        //Take the instantiated location and adjust transform
        SetCoords();

        //Choose correct sprite based on piece's name
        switch (this.name)
        {
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = "black"; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = "black"; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = "black"; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = "black"; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = "black"; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = "black"; break;
            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = "white"; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = "white"; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = "white"; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = "white"; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = "white"; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = "white"; break;
        }
    }

    public void SetCoords()
    {
        //Get the board value in order to convert to xy coords
        float x = xBoard;
        float y = yBoard;

        //Adjust by variable offset
        x *= 0.66f;
        y *= 0.66f;

        //Add constants (pos 0,0)
        x += -2.3f;
        y += -2.3f;

        //Set actual unity values
        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public int GetXBoard()
    {
        return xBoard;
    }

    public int GetYBoard()
    {
        return yBoard;
    }

    public void SetXBoard(int x)
    {
        xBoard = x;
    }

    public void SetYBoard(int y)
    {
        yBoard = y;
    }

    private void OnMouseUp()
    {
        if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player)
        {
            //Remove all moveplates relating to previously selected piece
            DestroyMovePlates();

            //Create new MovePlates
            InitiateMovePlates();
        }
    }

    // Disederhanakan, didelegasikan ke Game.cs
    public void DestroyMovePlates()
    {
        controller.GetComponent<Game>().DestroyAllMovePlates();
    }

    // BARU: Ditulis ulang untuk ChessDotNet
    public void InitiateMovePlates()
    {
        Game gameScript = controller.GetComponent<Game>();
        ChessGame logicBrain = gameScript.GetLogicBrain();

        // Dapatkan notasi "a1" dari posisi bidak ini
        string fromAlg = gameScript.GetAlgebraicFromCoords(xBoard, yBoard);
        Position fromPos = new Position(fromAlg);

        // Dapatkan semua langkah valid dari otak catur untuk bidak ini
        var validMoves = logicBrain.GetValidMoves(fromPos);

        // Loop untuk setiap langkah yang valid
        foreach (var move in validMoves)
        {
            string toAlg = move.NewPosition.ToString();
            Vector2Int toCoords = gameScript.GetCoordsFromAlgebraic(toAlg);

            // ----- TAMBAHKAN BARIS DEBUG INI -----
            Debug.Log($"Mencoba Cek Posisi: {toAlg} -> ({toCoords.x}, {toCoords.y})");
            // -------------------------------------

            GameObject pieceAtTarget = gameScript.GetPosition(toCoords.x, toCoords.y); // <-- Ini baris error

            if (pieceAtTarget != null)
            {
                // Ini adalah langkah memakan (Attack)
                MovePlateAttackSpawn(toCoords.x, toCoords.y);
            }
            else
            {
                // Ini adalah langkah normal (Move)
                MovePlateSpawn(toCoords.x, toCoords.y);
            }
        }
    }

    // Fungsi ini tidak berubah
    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;
        x *= 0.66f;
        y *= 0.66f;
        x += -2.3f;
        y += -2.3f;
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    // Fungsi ini tidak berubah
    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;
        x *= 0.66f;
        y *= 0.66f;
        x += -2.3f;
        y += -2.3f;
        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }
}