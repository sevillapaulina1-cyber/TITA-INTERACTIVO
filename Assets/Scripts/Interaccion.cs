using System.Collections;
using UnityEngine;
using TMPro;

public class CamInteractFearsToFathom : MonoBehaviour
{
    public LookAtFunction LookAtScript;

    public TextMeshProUGUI InteractionText;

    private float InteractDistance = 5f;
    public bool CanInteract = true;

    // UI
    public GameObject TalkPanel;
    public GameObject ChoicePack;
    public TextMeshProUGUI SubText;

    string holder;
    float time = 0.05f;

    void Update()
    {
        if (!CanInteract) return;

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, InteractDistance))
        {
            if (hit.collider.CompareTag("Nino"))
            {
                InteractionText.text = "Habla con el";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    CanInteract = false;
                    StartCoroutine(TalkToNiñoCO());
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

    IEnumerator TalkToNinoCO()
    {
        InteractionText.text = "";

        if (FpsController != null)
            FpsController.enabled = false;

        if (LookAtScript != null)
            LookAtScript.IKActive = true;

        yield return new WaitForSeconds(0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        TalkPanel.SetActive(true);

        yield return TypeText("Me: ", "Hello, are you OK ?");
        yield return MousePress();

        yield return TypeText("Maneq: ", "Yeah I'm fine.");
        yield return MousePress();

        yield return TypeText("Maneq: ", "Are you lost ?");
        yield return MousePress();

        yield return new WaitForSeconds(0.5f);

        ChoicePack.SetActive(true);
    }

    IEnumerator TypeText(string prefix, string message)
    {
        SubText.text = prefix;

        foreach (char c in message)
        {
            SubText.text += c;
            yield return new WaitForSeconds(time);
        }
    }

    public void Choice1Void()
    {
        StartCoroutine(Choice1CO());
    }

    public void Choice2Void()
    {
        StartCoroutine(Choice2CO());
    }

    IEnumerator Choice1CO()
    {
        ChoicePack.SetActive(false);

        yield return TypeText("Me: ", "No, I'm a local");

        yield return new WaitForSeconds(2f);

        yield return FinalCO();
    }

    IEnumerator Choice2CO()
    {
        ChoicePack.SetActive(false);

        yield return TypeText("Me: ", "Yes, I will ask for help later.");

        yield return new WaitForSeconds(2f);

        yield return FinalCO();
    }

    IEnumerator MousePress()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
    }
}