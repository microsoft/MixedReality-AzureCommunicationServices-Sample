using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.UX;

/// <summary>
/// This class adds some custom logic to toggle the pin buttons under special circumstances.
/// When another pin button controlling the same UI has been toggled or the user goes a certain distance from the UI.
/// </summary>
public class CustomPinButtonLogic : MonoBehaviour
{
    [Tooltip("The UI being controlled.")]
    [SerializeField]
    private MonoBehaviour UI;

    [Tooltip("The toggle buttons which control this UI.")]
    [SerializeField]
    private List<PressableButton> pinToggles = new List<PressableButton>();

    [Tooltip("The distance the user can travel from the UI before the button is unpinned.")]
    [SerializeField]
    private float maxDistance = 10.0f;

    private bool lastPinnedState = true;

    // Update is called once per frame
    void Update()
    {
        // If the UI is more than the max distance from the user, unpin it to enable the solver
        if (maxDistance < (UI.transform.position - Camera.main.transform.position).magnitude)
        {
            foreach (PressableButton button in pinToggles)
            {
                button.ForceSetToggled(false);
            }
            return;
        }
    
        // If the UI has changed state since last frame, ensure that all of the buttons reflect this change 
        if (lastPinnedState != UI.enabled)
        {
            foreach (PressableButton button in pinToggles)
            {
                // The button being toggled and the solver being enabled should be inverse
                if (button.IsToggled == UI.enabled)
                {
                    button.ForceSetToggled(!UI.enabled);
                };
            }
        }

        lastPinnedState = UI.enabled;
    }
}
