using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class SistemaDialogo : MonoBehaviour
{

    public Text InteractionText;

    private float InteractDistance = 5f;

    public bool CanInteract = true;

    // Controlador FPS (asigna tu script de movimiento aquí)
    public MonoBehaviour FirstPersonController;

    // UI
    public GameObject TalkPanel;
    public GameObject ChoicePack;
    public Text SubText;

    string holder;
    float time = 0.05f;

    void Start() { }

    void Update()
    {
        if (!CanInteract) return;

        Ray ray1 = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray1, out RaycastHit hit1, InteractDistance))
        {
            if (hit1.collider.CompareTag("Npc"))      // ← Tag cambiado a "Npc"
            {
                InteractionText.text = "Habla con el";

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    CanInteract = false;
                    StartCoroutine(TalkToNpcCO());
                }
            }
            else
            {
                InteractionText.text = "";
            }
        }
        else
        {
            InteractionText.text = "";
        }
    }

    IEnumerator TalkToNpcCO()
    {
        InteractionText.text = "";
        FirstPersonController.enabled = false;


        yield return new WaitForSeconds(1f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        TalkPanel.SetActive(true);

        yield return TypeText("Yo: ", "¡Hola!");
        yield return MousePress();


        yield return TypeText("Kid:", "Hola... ¿tu tambien estas jugando solo?");

        yield return new WaitForSeconds(1f);
        ChoicePack.SetActive(true);         // Activa el panel con las 3 opciones
    }

    IEnumerator TypeText(string speaker, string message)
    {
        SubText.text = speaker;
        foreach (char c in message)
        {
            SubText.text += c;
            yield return new WaitForSeconds(time);
        }
    }

    // ─── Opciones del jugador ────────────────────────────────────────────

    public void Choice1Void() => StartCoroutine(Choice1CO());   // Responder Si
    public void Choice2Void() => StartCoroutine(Choice2CO());   // Preguntar nivel
    public void Choice3Void() => StartCoroutine(Choice3CO());   // Preguntar edad

    IEnumerator Choice1CO()
    {
        ChoicePack.SetActive(false);
        yield return TypeText("a) ", "Si, ¿Quieres jugar juntos?");
        yield return new WaitForSeconds(3f);
        StartCoroutine(FinalCO());
    }

    IEnumerator Choice2CO()
    {
        ChoicePack.SetActive(false);
        yield return TypeText("b) ", "¿Que nivel eres?");
        yield return new WaitForSeconds(3f);
        StartCoroutine(FinalCO());
    }

    IEnumerator Choice3CO()                                     // ← Opción nueva
    {
        ChoicePack.SetActive(false);
        yield return TypeText("b) ", "¿Cuantos años tienes?");
        yield return new WaitForSeconds(3f);
        StartCoroutine(FinalCO());
    }

    // ─── Final ───────────────────────────────────────────────────────────

    IEnumerator FinalCO()
    {
        TalkPanel.SetActive(false);
        ChoicePack.SetActive(false);
        SubText.text = "";


        FirstPersonController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CanInteract = true;

        yield return null;
    }

    IEnumerator MousePress()
    {
        while (!Mouse.current.leftButton.wasPressedThisFrame)
            yield return null;
    }
}

